using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Application.Interface;
using Ecommerce.Application.Mapping;
using Ecommerce.Application.Validator;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.Service
{
    public class CustomerApplication : ICustomerApplication
    {
        //La capa Application orquesta el caso de uso y decide el límite transaccional:
        //registra los cambios en los repositorios y confirma una sola vez con SaveChangesAsync.
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly CustomerDtoValidator _validator;
        public CustomerApplication(IUnitOfWork unitOfWork, IMapper mapper, CustomerDtoValidator validator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _validator = validator;
        }
        public async Task<Response<bool>> AddAsync(CustomerDto customerDto, CancellationToken cancellationToken)
        {
            var response = new Response<bool>();
            try
            {
                var validationResult = await _validator.ValidateAsync(customerDto, cancellationToken);
                if(!validationResult.IsValid)
                {
                    response.IsSuccess = false;
                    response.Message = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
                    return response;
                }


                var customer = _mapper.Map<Customer>(customerDto);
                customer.Id = null; //El Id lo genera la base de datos (columna identity), forzamos a null para no tener problemas al insertar un nuevo registro.
                response.Data = await _unitOfWork.Customers.AddAsync(customer);
                if (response.Data) response.IsSuccess = true;
            }
            catch (Exception ex) { response.Message = ex.InnerException?.Message ?? ex.Message; }
            return response;
        }

        public async Task<Response<bool>> DeleteAsync(int id)
        {
            var response = new Response<bool>();
            try
            {
                response.Data = await _unitOfWork.Customers.DeleteAsync(id);
                if (response.Data) response.IsSuccess = true;
                else response.Message = $"Don't delete with ID {id}.";
            }
            catch (Exception ex) { response.Message = ex.InnerException?.Message ?? ex.Message; }
            return response;
        }

        public async Task<Response<IEnumerable<CustomerDto>>> GetAllAsync()
        {
            var response = new Response<IEnumerable<CustomerDto>>();
            try
            {
                var customers = await _unitOfWork.Customers.GetAllAsync();
                response.Data = _mapper.Map<IEnumerable<CustomerDto>>(customers);
                if (response.Data != null) response.IsSuccess = true;
            }
            catch (Exception ex) { response.Message = ex.InnerException?.Message ?? ex.Message; }
            return response;
        }

        public async Task<Response<CustomerDto?>> GetByIdAsync(int id)
        {
            var response = new Response<CustomerDto?>();
            try
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(id);
                response.Data = _mapper.Map<CustomerDto?>(customer);
                if (response.Data != null) response.IsSuccess = true;
                else
                {
                    response.IsSuccess = false;
                    response.Message = $"Don't find with ID {id}.";
                }
            }
            catch (Exception ex) { response.Message = ex.InnerException?.Message ?? ex.Message; }
            return response;
        }

        public async Task<Response<bool>> UpdateAsync(int id, CustomerDto customerDto)
        {
            var response = new Response<bool>();
            try
            {
                var existingCustomer = await _unitOfWork.Customers.GetByIdAsync(id);
              
                if(existingCustomer == null)
                {
                    response.IsSuccess = false;
                    response.Message = $"Customer with ID {id} not found.";
                    return response;
                }
                ManualMappingCustomer.MapInto(existingCustomer, customerDto);
                response.Data = await _unitOfWork.Customers.UpdateAsync(existingCustomer);
                if (response.Data) response.IsSuccess = true;
                else response.Message = $"Don't update with ID {existingCustomer.Id}.";
            }
            catch (Exception ex) { response.Message = ex.InnerException?.Message ?? ex.Message; }
            return response;
        }

        
    }
}
