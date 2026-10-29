using NSubstitute;
using NSubstitute.Core;
using NSubstitute.ExceptionExtensions;
using Ticketing.Command.Application.EventSourcing;
using Ticketing.Command.Domain.Aggregates;
using Ticketing.Command.Domain.Aggregates.Tickets;
using Ticketing.Command.Domain.Enums;
using Ticketing.Command.Domain.Exceptions;
using Ticketing.Command.Domain.Interfaces.Events;
using Ticketing.Common.Events;

namespace Ticketing.Command.UnitTest.Application.EventSourcing
{
    //Orquestación: el handler traduce entre el agregado y el almacén de eventos. Lo que depende de la BD real (versión, no encontrado, conflicto) va en integración
    public class EventSourcingHandlerTests
    {
        //xUnit crea una instancia nueva de la clase por cada test, así que estos dobles nacen vacíos en cada uno y no se comparten
        private readonly IEventStore _eventStore = Substitute.For<IEventStore>();
        private readonly IAggregateService<TicketAggregate> _aggregateService = Substitute.For<IAggregateService<TicketAggregate>>();

        [Theory]
        [InlineData(0)]
        [InlineData(3)]
        public async Task SaveAsync_ConCambiosPendientes_GuardaIdTipoEventosYVersionEsperada(int versionBeforeChanges)
        {
            //Arrange: un ticket con dos cambios pendientes; recojo lo que recibe el almacén al guardar
            TicketCreatedEvent createdEvent = new TicketCreatedEvent { Id = "ticket-1", OcurredAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc), OcurredBy = "ana@empresa.com", ErrorDetail = "Timeout al pagar" };
            TicketUpdatedEvent updatedEvent = new TicketUpdatedEvent { Id = "ticket-1", OcurredAt = new DateTime(2026, 1, 15, 11, 0, 0, DateTimeKind.Utc), OcurredBy = "luis@empresa.com", Status = "Closed" };
            List<EventBase> pendingEvents = new List<EventBase> { createdEvent, updatedEvent };
            TicketAggregate aggregate = new TicketAggregate("ticket-1", versionBeforeChanges + 2, versionBeforeChanges, pendingEvents, true);
            string? savedAggregateId = null;
            eAggregateType? savedAggregateType = null;
            IEnumerable<EventBase>? savedEvents = null;
            int? savedExpectedVersion = null;
            Action<CallInfo> rememberSavedArguments = call =>
            {
                savedAggregateId = call.ArgAt<string>(0);
                savedAggregateType = call.ArgAt<eAggregateType>(1);
                savedEvents = call.ArgAt<IEnumerable<EventBase>>(2);
                savedExpectedVersion = call.ArgAt<int>(3);
            };
            _eventStore
                .When(x => x.SaveEventsAsync(Arg.Any<string>(), Arg.Any<eAggregateType>(), Arg.Any<IEnumerable<EventBase>>(), Arg.Any<int>(), Arg.Any<CancellationToken>()))
                .Do(rememberSavedArguments);
            EventSourcingHandler<TicketAggregate> handler = new EventSourcingHandler<TicketAggregate>(_eventStore, _aggregateService);

            //Act
            await handler.SaveAsync(aggregate, CancellationToken.None);

            //Assert: el almacén recibe el id, el tipo, los dos eventos en orden y la versión que tenía el agregado antes de los cambios
            Assert.Equal("ticket-1", savedAggregateId);
            Assert.Equal(eAggregateType.TicketAggregate, savedAggregateType);
            Assert.Equal(pendingEvents, savedEvents);
            Assert.Equal(versionBeforeChanges, savedExpectedVersion);
        }

        [Fact]
        public async Task SaveAsync_ConflictoDeVersion_PropagaLaConcurrencyException()
        {
            //Arrange: el almacén detecta que otra operación guardó antes y lanza el conflicto
            ConcurrencyException conflict = new ConcurrencyException("ticket-1");
            _eventStore
                .SaveEventsAsync(Arg.Any<string>(), Arg.Any<eAggregateType>(), Arg.Any<IEnumerable<EventBase>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(conflict);
            List<EventBase> pendingEvents = new List<EventBase> { new TicketCreatedEvent { Id = "ticket-1", OcurredAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc), OcurredBy = "ana@empresa.com", ErrorDetail = "Timeout al pagar" } };
            TicketAggregate aggregate = new TicketAggregate("ticket-1", 1, 0, pendingEvents, true);
            EventSourcingHandler<TicketAggregate> handler = new EventSourcingHandler<TicketAggregate>(_eventStore, _aggregateService);

            //Act
            Exception? thrown = await Record.ExceptionAsync(() => handler.SaveAsync(aggregate, CancellationToken.None));

            //Assert: llega la misma excepción, sin traducir ni tragarse
            Assert.Same(conflict, thrown);
        }

        [Fact]
        public async Task SaveAsync_TokenCancelado_LanzaOperationCanceled()
        {
            //Arrange: token ya cancelado; el almacén, como cualquier dependencia que respeta el token, lanza al recibirlo
            _eventStore
                .SaveEventsAsync(Arg.Any<string>(), Arg.Any<eAggregateType>(), Arg.Any<IEnumerable<EventBase>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new OperationCanceledException());
            List<EventBase> pendingEvents = new List<EventBase> { new TicketCreatedEvent { Id = "ticket-1", OcurredAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc), OcurredBy = "ana@empresa.com", ErrorDetail = "Timeout al pagar" } };
            TicketAggregate aggregate = new TicketAggregate("ticket-1", 1, 0, pendingEvents, true);
            EventSourcingHandler<TicketAggregate> handler = new EventSourcingHandler<TicketAggregate>(_eventStore, _aggregateService);
            CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            //Act
            Exception? thrown = await Record.ExceptionAsync(() => handler.SaveAsync(aggregate, cancellation.Token));

            //Assert: llega una cancelación (cualquier subtipo)
            Assert.IsAssignableFrom<OperationCanceledException>(thrown);
        }

        [Theory]
        [InlineData("ticket-1")]
        [InlineData("ticket-2")]
        public async Task GetByIdAsync_ConEventosGuardados_DevuelveElAgregadoQueReconstruyeElServicio(string aggregateId)
        {
            //Arrange: el almacén devuelve los eventos de ese id y el servicio del agregado los convierte en un agregado
            List<EventBase> storedEvents = new List<EventBase> { new TicketCreatedEvent { Id = "ticket-1", OcurredAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc), OcurredBy = "ana@empresa.com", ErrorDetail = "Timeout al pagar" } };
            TicketAggregate rebuiltAggregate = new TicketAggregate(aggregateId, 1, 1, new List<EventBase>(), true);
            _eventStore.GetEventsAsync(aggregateId, Arg.Any<CancellationToken>()).Returns(storedEvents);
            _aggregateService.ReplayEvents(storedEvents).Returns(rebuiltAggregate);
            EventSourcingHandler<TicketAggregate> handler = new EventSourcingHandler<TicketAggregate>(_eventStore, _aggregateService);

            //Act
            TicketAggregate result = await handler.GetByIdAsync(aggregateId, CancellationToken.None);

            //Assert: devuelve exactamente el agregado reconstruido
            Assert.Same(rebuiltAggregate, result);
        }

        [Fact]
        public async Task GetByIdAsync_TokenCancelado_LanzaOperationCanceled()
        {
            //Arrange: token ya cancelado; el almacén, como cualquier dependencia que respeta el token, lanza al recibirlo
            _eventStore
                .GetEventsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new OperationCanceledException());
            EventSourcingHandler<TicketAggregate> handler = new EventSourcingHandler<TicketAggregate>(_eventStore, _aggregateService);
            CancellationTokenSource cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            //Act
            Exception? thrown = await Record.ExceptionAsync(() => handler.GetByIdAsync("ticket-1", cancellation.Token));

            //Assert: llega una cancelación (cualquier subtipo)
            Assert.IsAssignableFrom<OperationCanceledException>(thrown);
        }
    }
}
