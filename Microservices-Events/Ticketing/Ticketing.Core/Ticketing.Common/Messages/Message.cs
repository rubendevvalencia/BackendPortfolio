namespace Ticketing.Common.Messages
{
    public abstract class Message
    {
        public required string Id {get; set;}
        public required DateTime OcurredAt { get; set;}
        public required string? OcurredBy { get; set; }

        protected Message(){}

    }
    
}


