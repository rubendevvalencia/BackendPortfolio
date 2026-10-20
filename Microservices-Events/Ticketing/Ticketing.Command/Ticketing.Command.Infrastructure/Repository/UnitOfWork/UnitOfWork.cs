using Ticketing.Command.Domain.Interface.EventModels;
using Ticketing.Command.Domain.Interface.Repository.UnitOfWork;
using Ticketing.Command.Infrastructure.Data;

namespace Ticketing.Command.Infrastructure.Repository.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MongoDbContext _dbContext;
        private readonly IEventModelRepository _eventModelRepo;

        public UnitOfWork(MongoDbContext dbContext, IEventModelRepository eventModelRepository)
        {
            _dbContext = dbContext; 
            _eventModelRepo = eventModelRepository;
        }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
