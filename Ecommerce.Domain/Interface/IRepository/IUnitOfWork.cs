using Ecommerce.Application.Interface.Jwt;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Interface.IRepository
{
    public interface IUnitOfWork
    {
        //Trabaja como un contenedor para todos los repositorios, permitiendo que se realicen operaciones de manera coordinada y asegurando la consistencia de los datos.
        ICustomerRepository _customers { get; } // Add other repositories here as needed
        IUserRepository _user { get; }
        IGenerateToken _genJwt { get; }
    }
}
