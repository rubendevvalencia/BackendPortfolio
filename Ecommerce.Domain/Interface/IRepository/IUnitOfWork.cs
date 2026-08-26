using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Interface.IRepository
{
    public interface IUnitOfWork
    {
        //Trabaja como un contenedor para todos los repositorios, permitiendo que se realicen operaciones de manera coordinada y asegurando la consistencia de los datos.
        ICustomerRepository Customers { get; } // Add other repositories here as needed

        //Confirma en bloque todos los cambios registrados en los repositorios durante el caso de uso.
        //Devuelve el número de filas afectadas.
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
