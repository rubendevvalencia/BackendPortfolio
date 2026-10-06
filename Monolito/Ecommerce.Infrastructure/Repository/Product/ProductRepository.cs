using Microsoft.EntityFrameworkCore;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Domain.Interface.IRepository.IProduct;

namespace Ecommerce.Infrastructure.Repository
{
    //Version del repositorio de Customer bajo el patron Unit of Work (v2 de la API).
    //No confirma en ningun metodo: el unico SaveChangesAsync de la Infraestructura vive en UnitOfWork.
    public class ProductRepositoryUoW : IProductRepository
    {
        private readonly DbContextEF _dbContext;
        public ProductRepositoryUoW(DbContextEF dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var product = await _dbContext.Products.FindAsync(new object?[] { id }, cancellationToken);
            return product;
        }

        public async Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var products = await _dbContext.Products.AsNoTracking().ToListAsync(cancellationToken);
            return products;
        }

        public async Task AddAsync(Product entity, CancellationToken cancellationToken = default)
        {
            await _dbContext.Products.AddAsync(entity, cancellationToken);
        }

        public void Update(Product entity)
        {
            if (_dbContext.Entry(entity).State == EntityState.Detached) _dbContext.Products.Update(entity);
        }

        public void Delete(Product entity)
        {
            _dbContext.Products.Remove(entity);
        }

        public async Task<bool> CompareInfoInDb(Product entity, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Products.AnyAsync(o =>
                o.Name == entity.Name &&
                o.Description == entity.Description &&
                o.Price == entity.Price &&
                o.StockQuantity == entity.StockQuantity &&
                o.CategoryId == entity.CategoryId, 
                cancellationToken);
        }
    }
}
