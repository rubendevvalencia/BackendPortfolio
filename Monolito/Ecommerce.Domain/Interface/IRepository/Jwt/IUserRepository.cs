using Ecommerce.Domain.Entities.Jwt;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Interface.IRepository.Jwt
{
    //Mismo contrato que IBaseRepositoryUoW: el repositorio NO confirma, solo registra la
    //intencion de cambio. Quien confirma es el caso de uso, con IUnitOfWork.SaveChangesAsync().
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUserNameAsync(string userName);

        Task CreateUserAsync(User user, string password);

        //Sincrono: comparar un hash no toca la base de datos ni hace ninguna espera real.
        bool CheckPass(User user, string password);
    }
}
