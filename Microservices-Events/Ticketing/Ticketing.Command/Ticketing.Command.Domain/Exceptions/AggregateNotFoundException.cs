namespace Ticketing.Command.Domain.Exceptions
{
    //No hay ningún evento guardado para ese agregado
    public class AggregateNotFoundException : Exception
    {
        public string AggregateId { get; }

        public AggregateNotFoundException(string aggregateId)
            : base($"No existe el agregado {aggregateId}.")
        {
            AggregateId = aggregateId;
        }
    }
}
