using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Interface
{
    public interface ICustomerUoW : IDisposable
    {
        ICustomerRepository Customers { get; }
    }
}
