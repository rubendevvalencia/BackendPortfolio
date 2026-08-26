using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Interface.IRepository
{
    public interface IUnitOfWork : IDisposable
    {
        //Trabaja como un contenedor para todos los repositorios, permitiendo que se realicen operaciones de manera coordinada y asegurando la consistencia de los datos.
        ICustomerRepository Customers { get; } // Add other repositories here as needed
    }
}
