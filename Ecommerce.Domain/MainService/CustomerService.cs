using Ecommerce.Domain.Entity;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Domain.Interface.IService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.MainService
{
    public class CustomerService : ICustomerService
    { 
        private readonly IUnitOfWork _unitOfWork; 
        //Ayuda a las transacciones y a la gestión de los repositorios de forma atómica, 
        //asegurando que todas las operaciones se realicen de manera coordinada y consistente.
        public CustomerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public Task<bool> AddAsync(Customer customer)
        {
            return _unitOfWork.Customers.AddAsync(customer);
        }

        public Task<bool> DeleteAsync(int id)
        {
            return _unitOfWork.Customers.DeleteAsync(id);
        }

        public Task<IEnumerable<Customer>> GetAllAsync()
        {
            return _unitOfWork.Customers.GetAllAsync();
        }

        public Task<Customer?> GetByIdAsync(int id)
        {
            return _unitOfWork.Customers.GetByIdAsync(id);
        }

        public Task<bool> UpdateAsync(Customer customer)
        {
            return _unitOfWork.Customers.UpdateAsync(customer);
        }
    }
}
