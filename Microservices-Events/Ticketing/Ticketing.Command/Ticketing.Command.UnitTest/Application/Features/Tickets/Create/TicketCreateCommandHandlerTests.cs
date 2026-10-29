using NSubstitute;
using NSubstitute.Core;
using Ticketing.Command.Application.Features.Tickets.Create;
using Ticketing.Command.Domain.Aggregates.Tickets;
using Ticketing.Command.Domain.Interfaces;
using Ticketing.Command.Domain.Interfaces.Events;
using Ticketing.Command.Domain.Services;
using Ticketing.Common.Common.Response;
using Ticketing.Common.Events;

namespace Ticketing.Command.UnitTest.Application.Features.Tickets.Create
{
    //Orquestación del caso de uso: el handler fija la transacción. El servicio del agregado es el real (lógica pura, sin E/S)
    public class TicketCreateCommandHandlerTests
    {
        //xUnit crea una instancia nueva de la clase por cada test, así que estos dobles nacen vacíos en cada uno y no se comparten
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IEventSourcingHandler<TicketAggregate> _eventSourcingHandler = Substitute.For<IEventSourcingHandler<TicketAggregate>>();

        [Fact]
        public async Task Handle_DatosValidos_DevuelveSuccessConElIdDelTicketGuardado()
        {
            //Arrange: SaveAsync es una salida; cuando se llama, guardo en 'saved' el ticket que recibe para poder mirarlo después
            TicketAggregate? saved = null;
            Action<CallInfo> rememberSavedTicket = call => saved = call.Arg<TicketAggregate>();
            _eventSourcingHandler
                .When(x => x.SaveAsync(Arg.Any<TicketAggregate>(), Arg.Any<CancellationToken>()))
                .Do(rememberSavedTicket);
            TicketAggregateService ticketAggregateService = new TicketAggregateService();
            TicketCreateCommandHandler handler = new TicketCreateCommandHandler(_unitOfWork, ticketAggregateService, _eventSourcingHandler);
            TicketCreateCommand command = new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = "Timeout", ErrorDetail = "Timeout al pagar" };

            //Act
            Response<string> response = await handler.Handle(command, CancellationToken.None);

            //Assert: éxito y el dato devuelto es el id del ticket que se guardó
            Assert.NotNull(saved);
            string savedId = saved.Id;
            string? returnedId = response.Data;
            bool isSuccess = response.IsSuccess;
            Assert.True(isSuccess);
            Assert.Equal(savedId, returnedId);
        }

        [Theory]
        [InlineData("ana@empresa.com", "Timeout al pagar")]
        [InlineData("luis@empresa.com", "NullReference en carrito")]
        public async Task Handle_DatosValidos_GuardaUnTicketConLosDatosDelCommand(string userName, string errorDetail)
        {
            //Arrange: SaveAsync es una salida; cuando se llama, guardo en 'saved' el ticket que recibe para poder mirarlo después
            TicketAggregate? saved = null;
            Action<CallInfo> rememberSavedTicket = call => saved = call.Arg<TicketAggregate>();
            _eventSourcingHandler
                .When(x => x.SaveAsync(Arg.Any<TicketAggregate>(), Arg.Any<CancellationToken>()))
                .Do(rememberSavedTicket);
            TicketAggregateService ticketAggregateService = new TicketAggregateService();
            TicketCreateCommandHandler handler = new TicketCreateCommandHandler(_unitOfWork, ticketAggregateService, _eventSourcingHandler);
            TicketCreateCommand command = new TicketCreateCommand { UserName = userName, ErrorType = "Timeout", ErrorDetail = errorDetail };

            //Act
            await handler.Handle(command, CancellationToken.None);

            //Assert: un ticket guardado con un único evento pendiente, con los datos del command
            Assert.NotNull(saved);
            int pendingCount = saved.UncommittedChanges.Count;
            Assert.Equal(1, pendingCount);
            EventBase pendingEvent = saved.UncommittedChanges.First();
            eEventType eventType = pendingEvent.Type;
            Assert.Equal(eEventType.TicketCreatedEvent, eventType);
            TicketCreatedEvent createdEvent = (TicketCreatedEvent)pendingEvent;
            string? occurredBy = createdEvent.OcurredBy;
            string? savedErrorDetail = createdEvent.ErrorDetail;
            Assert.Equal(userName, occurredBy);
            Assert.Equal(errorDetail, savedErrorDetail);
        }

        [Fact]
        public async Task Handle_DatosValidos_AbreGuardaYConfirmaLaTransaccion()
        {
            //Arrange: cada llamada importante apunta su nombre en una lista, para ver en qué orden se hicieron
            List<string> calls = new List<string>();
            Action<CallInfo> noteBegin = call => calls.Add("Begin");
            Action<CallInfo> noteSave = call => calls.Add("Save");
            Action<CallInfo> noteCommit = call => calls.Add("Commit");
            Action<CallInfo> noteRollback = call => calls.Add("Rollback");
            _unitOfWork
                .When(x => x.BeginTransactionAsync(Arg.Any<CancellationToken>()))
                .Do(noteBegin);
            _eventSourcingHandler
                .When(x => x.SaveAsync(Arg.Any<TicketAggregate>(), Arg.Any<CancellationToken>()))
                .Do(noteSave);
            _unitOfWork
                .When(x => x.CommitTransactionAsync(Arg.Any<CancellationToken>()))
                .Do(noteCommit);
            _unitOfWork
                .When(x => x.RollbackAsync(Arg.Any<CancellationToken>()))
                .Do(noteRollback);
            TicketAggregateService ticketAggregateService = new TicketAggregateService();
            TicketCreateCommandHandler handler = new TicketCreateCommandHandler(_unitOfWork, ticketAggregateService, _eventSourcingHandler);
            TicketCreateCommand command = new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = "Timeout", ErrorDetail = "Timeout al pagar" };

            //Act
            await handler.Handle(command, CancellationToken.None);

            //Assert: abre, guarda y confirma, en ese orden y una sola vez cada cosa, y no aborta
            List<string> expectedCalls = new List<string> { "Begin", "Save", "Commit" };
            Assert.Equal(expectedCalls, calls);
        }

        [Fact]
        public async Task Handle_FallaElGuardado_AbortaLaTransaccionYRelanzaLaExcepcion()
        {
            //Arrange: el guardado de eventos falla; apunto las llamadas a la unidad de trabajo
            List<string> calls = new List<string>();
            Action<CallInfo> noteBegin = call => calls.Add("Begin");
            Action<CallInfo> noteCommit = call => calls.Add("Commit");
            Action<CallInfo> noteRollback = call => calls.Add("Rollback");
            _unitOfWork
                .When(x => x.BeginTransactionAsync(Arg.Any<CancellationToken>()))
                .Do(noteBegin);
            _unitOfWork
                .When(x => x.CommitTransactionAsync(Arg.Any<CancellationToken>()))
                .Do(noteCommit);
            _unitOfWork
                .When(x => x.RollbackAsync(Arg.Any<CancellationToken>()))
                .Do(noteRollback);
            InvalidOperationException saveFailure = new InvalidOperationException("fallo simulado al guardar");
            _eventSourcingHandler
                .SaveAsync(Arg.Any<TicketAggregate>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromException(saveFailure));
            TicketAggregateService ticketAggregateService = new TicketAggregateService();
            TicketCreateCommandHandler handler = new TicketCreateCommandHandler(_unitOfWork, ticketAggregateService, _eventSourcingHandler);
            TicketCreateCommand command = new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = "Timeout", ErrorDetail = "Timeout al pagar" };

            //Act
            Exception? thrown = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));

            //Assert: llega la misma excepción; abrió y abortó, y no confirmó
            List<string> expectedCalls = new List<string> { "Begin", "Rollback" };
            Assert.Equal(saveFailure, thrown);
            Assert.Equal(expectedCalls, calls);
        }

        [Fact]
        public async Task Handle_TokenCancelado_LanzaOperationCanceledYNoGuardaNada()
        {
            //Arrange: token ya cancelado; la unidad de trabajo, como cualquier dependencia que respeta el token, lanza al recibirlo
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            _unitOfWork
                .BeginTransactionAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromCanceled(cancellationTokenSource.Token));
            List<string> calls = new List<string>();
            Action<CallInfo> noteSave = call => calls.Add("Save");
            Action<CallInfo> noteCommit = call => calls.Add("Commit");
            _eventSourcingHandler
                .When(x => x.SaveAsync(Arg.Any<TicketAggregate>(), Arg.Any<CancellationToken>()))
                .Do(noteSave);
            _unitOfWork
                .When(x => x.CommitTransactionAsync(Arg.Any<CancellationToken>()))
                .Do(noteCommit);
            TicketAggregateService ticketAggregateService = new TicketAggregateService();
            TicketCreateCommandHandler handler = new TicketCreateCommandHandler(_unitOfWork, ticketAggregateService, _eventSourcingHandler);
            TicketCreateCommand command = new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = "Timeout", ErrorDetail = "Timeout al pagar" };

            //Act
            Exception? thrown = await Record.ExceptionAsync(() => handler.Handle(command, cancellationTokenSource.Token));

            //Assert: llega una cancelación (cualquier subtipo) y no se guardó ni confirmó nada
            bool isCancellation = thrown is OperationCanceledException;
            List<string> expectedCalls = new List<string>();
            Assert.True(isCancellation);
            Assert.Equal(expectedCalls, calls);
        }

        [Fact]
        public async Task Handle_FallaLaConfirmacion_AbortaLaTransaccionYRelanzaLaExcepcion()
        {
            //Arrange: la confirmación de la transacción falla; apunto el resto de llamadas a la unidad de trabajo
            List<string> calls = new List<string>();
            Action<CallInfo> noteBegin = call => calls.Add("Begin");
            Action<CallInfo> noteRollback = call => calls.Add("Rollback");
            _unitOfWork
                .When(x => x.BeginTransactionAsync(Arg.Any<CancellationToken>()))
                .Do(noteBegin);
            _unitOfWork
                .When(x => x.RollbackAsync(Arg.Any<CancellationToken>()))
                .Do(noteRollback);
            InvalidOperationException commitFailure = new InvalidOperationException("fallo simulado al confirmar");
            _unitOfWork
                .CommitTransactionAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromException(commitFailure));
            TicketAggregateService ticketAggregateService = new TicketAggregateService();
            TicketCreateCommandHandler handler = new TicketCreateCommandHandler(_unitOfWork, ticketAggregateService, _eventSourcingHandler);
            TicketCreateCommand command = new TicketCreateCommand { UserName = "ana@empresa.com", ErrorType = "Timeout", ErrorDetail = "Timeout al pagar" };

            //Act
            Exception? thrown = await Record.ExceptionAsync(() => handler.Handle(command, CancellationToken.None));

            //Assert: llega la misma excepción; abrió y abortó
            List<string> expectedCalls = new List<string> { "Begin", "Rollback" };
            Assert.Equal(commitFailure, thrown);
            Assert.Equal(expectedCalls, calls);
        }
    }
}
