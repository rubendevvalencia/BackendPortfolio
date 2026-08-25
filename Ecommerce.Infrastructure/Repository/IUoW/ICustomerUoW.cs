using Ecommerce.Domain.Interface.IRepository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Interface.IUoW
{
    public interface ICustomerUoW : IDisposable
    {
        ICustomerRepository Customers { get; }
    }
}
