using Ticketing.Query.Domain.Entities;

namespace Ticketing.Query.Domain.Interfaces
{
    public interface IEmployeeRepository : IBaseRepository<Employee>
    {
        Task<Employee?> GetByUsernameAsync(string userName);
        
    }
    
}


