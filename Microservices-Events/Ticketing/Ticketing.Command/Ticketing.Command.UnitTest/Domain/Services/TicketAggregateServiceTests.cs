using Ticketing.Command.Domain.Aggregates.Tickets;
using Ticketing.Command.Domain.Services;
using Ticketing.Common.Events;

namespace Ticketing.Command.UnitTest.Domain.Services
{
    //Unitario de lógica sobre el servicio del agregado: si la creación pasa al handler, estos tests se mueven con ella
    public class TicketAggregateServiceTests
    {
        [Fact]
        public void Create_DatosValidos_DevuelveTicketActivoEnVersion1()
        {
            //Arrange: servicio sin dependencias
            TicketAggregateService service = new TicketAggregateService();

            //Act
            TicketAggregate ticket = service.Create("ana@empresa.com", "Timeout", "Timeout al pagar");

            //Assert: activo, una versión nueva y nada guardado todavía
            Assert.True(ticket.Active);
            Assert.Equal(1, ticket.Version);
            Assert.Equal(0, ticket.VersionBeforeChanges);
        }

        [Theory]
        [InlineData("ana@empresa.com", "Timeout al pagar")]
        [InlineData("luis@empresa.com", "NullReference en carrito")]
        public void Create_DatosValidos_ApuntaUnTicketCreatedEventConLosDatosRecibidos(string userName, string errorDetail)
        {
            //Arrange: servicio sin dependencias
            TicketAggregateService service = new TicketAggregateService();

            //Act
            TicketAggregate ticket = service.Create(userName, "Timeout", errorDetail);

            //Assert: un único evento pendiente, del tipo de creación y con los datos de la entrada
            EventBase pendingEvent = Assert.Single(ticket.UncommittedChanges);
            TicketCreatedEvent createdEvent = Assert.IsType<TicketCreatedEvent>(pendingEvent);
            Assert.Equal(eEventType.TicketCreatedEvent, createdEvent.Type);
            Assert.Equal(userName, createdEvent.OcurredBy);
            Assert.Equal(errorDetail, createdEvent.ErrorDetail);
        }

        [Fact]
        public void Create_DatosValidos_ElIdDelTicketEsElDelEventoYEsGuidV7()
        {
            //Arrange: servicio sin dependencias
            TicketAggregateService service = new TicketAggregateService();

            //Act
            TicketAggregate ticket = service.Create("ana@empresa.com", "Timeout", "Timeout al pagar");

            //Assert: el id lo decide el dominio y viaja en el evento
            EventBase pendingEvent = Assert.Single(ticket.UncommittedChanges);
            Assert.Equal(ticket.Id, pendingEvent.Id);
            Guid parsedId = Guid.Parse(ticket.Id);
            Assert.Equal(7, parsedId.Version);
        }

        [Fact]
        public void Create_DosLlamadas_GeneranIdsDistintos()
        {
            //Arrange: servicio sin dependencias
            TicketAggregateService service = new TicketAggregateService();

            //Act
            TicketAggregate first = service.Create("ana@empresa.com", "Timeout", "Timeout al pagar");
            TicketAggregate second = service.Create("ana@empresa.com", "Timeout", "Timeout al pagar");

            //Assert: mismos datos, tickets distintos
            Assert.NotEqual(first.Id, second.Id);
        }

        [Fact]
        public void Create_ReproducirSusEventos_DaElMismoTicket()
        {
            //Arrange: ticket recién creado con su evento pendiente
            TicketAggregateService service = new TicketAggregateService();
            TicketAggregate created = service.Create("ana@empresa.com", "Timeout", "Timeout al pagar");

            //Act
            TicketAggregate replayed = service.ReplayEvents(created.UncommittedChanges);

            //Assert: mismo ticket, con el evento ya contado como guardado
            Assert.Equal(created.Id, replayed.Id);
            Assert.True(replayed.Active);
            Assert.Equal(1, replayed.Version);
            Assert.Equal(1, replayed.VersionBeforeChanges);
            Assert.Empty(replayed.UncommittedChanges);
        }

        [Fact]
        public void Create_DatosValidos_OcurredAtEnUtc()
        {
            //Arrange: servicio sin dependencias
            TicketAggregateService service = new TicketAggregateService();

            //Act
            TicketAggregate ticket = service.Create("ana@empresa.com", "Timeout", "Timeout al pagar");

            //Assert: solo lo que no depende del reloj real
            EventBase pendingEvent = Assert.Single(ticket.UncommittedChanges);
            Assert.Equal(DateTimeKind.Utc, pendingEvent.OcurredAt.Kind);
        }

        [Fact]
        public void Create_ErrorDetailNulo_EventoConErrorDetailNulo()
        {
            //Arrange: servicio sin dependencias
            TicketAggregateService service = new TicketAggregateService();

            //Act
            TicketAggregate ticket = service.Create("ana@empresa.com", "Timeout", null);

            //Assert: el ticket se crea y el evento lleva el detalle vacío
            EventBase pendingEvent = Assert.Single(ticket.UncommittedChanges);
            TicketCreatedEvent createdEvent = Assert.IsType<TicketCreatedEvent>(pendingEvent);
            Assert.True(ticket.Active);
            Assert.Null(createdEvent.ErrorDetail);
        }
    }
}
