namespace Ticketing.Common.Events
{
    //Nombres de los topics de Kafka: Command publica y Query consume, así que el nombre es parte del contrato compartido
    public static class KafkaTopics
    {
        public const string TicketEvents = "KAFKA_TOPIC";
    }
}
