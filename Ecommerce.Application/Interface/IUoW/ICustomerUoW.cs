using Ecommerce.Application.Interface.IRepository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Interface.IUoW
{
    public interface ICustomerUoW : IDisposable
    {
        ICustomerRepository Customers { get; }
    }
}
