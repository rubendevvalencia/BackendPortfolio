namespace Ticketing.Query.Domain.Entities.Audit
{
    public abstract class BaseAudit
    {
        public Guid Id {get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime LastUpdatedAt { get; set;}
        public string? LastUpdatedBy { get; set; }
        public bool IsDeleted {get; set;} = false;
        public DateTime DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

    }
}
