using Ecommerce.Domain.Interface.IRepository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        public ICustomerRepository Customers { get; }
        public UnitOfWork(ICustomerRepository customerRepository) => Customers = customerRepository;

        public void Dispose() => System.GC.SuppressFinalize(this);
    }
}
