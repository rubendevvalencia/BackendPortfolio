using Ecommerce.Application.Interface.IUoW;
using Ecommerce.Domain.Interface.IRepository;
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
