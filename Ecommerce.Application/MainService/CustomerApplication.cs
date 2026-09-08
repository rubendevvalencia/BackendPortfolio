using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Application.Interface;
using Ecommerce.Application.Mapping;
using Ecommerce.Application.Validator;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using FluentValidation;
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
        private readonly ICustomerRepository _customerRepo;
        private readonly IMapper _mapper;
        //Se depende de la abstraccion IValidator<CustomerDto>, no de la clase concreta:
        //el servicio no conoce la implementacion y en los tests se puede sustituir por un doble.
        private readonly IValidator<CustomerDto> _validator; //Se inyecta el validador de CustomerDto la abstracción en vez de la implementación, que es el que contiene las reglas de validación para la entidad Customer.

        public CustomerApplication(ICustomerRepository customerRepo, IMapper mapper, IValidator<CustomerDto> validator)
        {
            _customerRepo = customerRepo;
            _mapper = mapper;
            _validator = validator;
        }
        //Middleware usado para manejos de excepciones, quitamos todos los try/catch
        public async Task<Response<bool>> AddAsync(CustomerDto customerDto, CancellationToken cancellationToken)
        {
            var response = new Response<bool>();
            
            var validationResult = await _validator.ValidateAsync(customerDto, cancellationToken);
            if (!validationResult.IsValid) return validationResult.ToFailedResponse<bool>();

            var customer = _mapper.Map<Customer>(customerDto);
            customer.Id = null; //El Id lo genera la base de datos (columna identity), forzamos a null para no tener problemas al insertar un nuevo registro.
            response.Data = await _customerRepo.AddAsync(customer);
            if (response.Data) response.IsSuccess = true;
            else return Response<bool>.Fail("Customer could not be added.");
          
            return response;
        }

        public async Task<Response<bool>> DeleteAsync(int id)
        {
            var response = new Response<bool>();
            
            response.Data = await _customerRepo.DeleteAsync(id);
            if (response.Data) response.IsSuccess = true;
            else return Response<bool>.NotFound($"Customer with ID {id} not found.");
          
            return response;
        }

        public async Task<Response<IEnumerable<CustomerDto>>> GetAllAsync()
        {
            var customers = await _customerRepo.GetAllAsync();
                return Response<IEnumerable<CustomerDto>>.Success(_mapper.Map<IEnumerable<CustomerDto>>(customers));
            
        }

        public async Task<Response<CustomerDto?>> GetByIdAsync(int id)
        {
            var customer = await _customerRepo.GetByIdAsync(id);
            if (customer == null) return Response<CustomerDto?>.NotFound($"Customer with ID {id} not found.");
            return Response<CustomerDto?>.Success(_mapper.Map<CustomerDto?>(customer));
        }

        public async Task<Response<bool>> UpdateAsync(int id, CustomerDto customerDto, CancellationToken cancellationToken)
        {
            var response = new Response<bool>();
           
            //El DTO de entrada se valida igual que en el alta: es la misma forma y las mismas reglas.
            var validationResult = await _validator.ValidateAsync(customerDto, cancellationToken);
            if (!validationResult.IsValid) return validationResult.ToFailedResponse<bool>();

            var existingCustomer = await _customerRepo.GetByIdAsync(id);
            if (existingCustomer == null) return Response<bool>.NotFound($"Customer with ID {id} not found.");
            ManualMappingCustomer.MapInto(existingCustomer, customerDto);
            response.Data = await _customerRepo.UpdateAsync(existingCustomer);
            if (response.Data) response.IsSuccess = true;
            else return Response<bool>.Fail($"Customer with ID {id} could not be updated.");
           
            return response;
        }
    }
}
