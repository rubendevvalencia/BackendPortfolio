using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Infrastructure.Interface.IUoW;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Repository
{
    public class CustomerUoW : ICustomerUoW
    {
        public CustomerUoW(ICustomerRepository customerRepository) => Customers = customerRepository;
        public ICustomerRepository Customers { get; }

        public void Dispose()
        {
            System.GC.SuppressFinalize(this);
        }
    }
}
