namespace Ticketing.Query.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        public IEmployeeRepository _employeesRepo {get;} 
      
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
    
}


