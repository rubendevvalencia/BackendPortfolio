namespace Ticketing.Query.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        public IEmployeeRepository _employeesRepo {get;} 
        public ITicketsRepository _ticketsRepo {get; }
      
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
    
}


