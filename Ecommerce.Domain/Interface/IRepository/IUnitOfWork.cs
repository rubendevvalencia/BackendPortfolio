using Ecommerce.Domain.Interface.IRepository.Jwt;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Interface.IRepository
{
    public interface IUnitOfWork
    {
        //Trabaja como un contenedor para todos los repositorios, permitiendo que se realicen operaciones de manera coordinada y asegurando la consistencia de los datos.
        ICustomerRepositoryUoW _customersUoW {get;}
        IUserRepository _user { get; }
    }
}
