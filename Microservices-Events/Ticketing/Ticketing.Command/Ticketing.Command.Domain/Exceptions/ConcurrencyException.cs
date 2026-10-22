namespace Ticketing.Command.Domain.Exceptions
{
    //Otra operación guardó eventos del mismo agregado antes que esta
    public class ConcurrencyException : Exception
    {
        public string AggregateId { get; }

        public ConcurrencyException(string aggregateId)
            : base($"El agregado {aggregateId} fue modificado por otra operación.")
        {
            AggregateId = aggregateId;
        }

        public ConcurrencyException(string aggregateId, Exception innerException)
            : base($"El agregado {aggregateId} fue modificado por otra operación.", innerException)
        {
            AggregateId = aggregateId;
        }
    }
}
