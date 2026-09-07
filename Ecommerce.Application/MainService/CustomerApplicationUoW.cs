using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Application.Interface;
using Ecommerce.Application.Mapping;
using Ecommerce.Application.Validator;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using FluentValidation;

namespace Ecommerce.Application.MainService
{
    public class CustomerApplicationUoW : ICustomerApplicationUoW
    {
         //La capa Application orquesta el caso de uso y decide el límite transaccional:
        //registra los cambios en los repositorios y confirma una sola vez con SaveChangesAsync.
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        //Se depende de la abstraccion IValidator<CustomerDto>, no de la clase concreta:
        //el servicio no conoce la implementacion y en los tests se puede sustituir por un doble.
        private readonly IValidator<CustomerDto> _validator; //Se inyecta el validador de CustomerDto la abstracción en vez de la implementación, que es el que contiene las reglas de validación para la entidad Customer.

        public CustomerApplicationUoW(IUnitOfWork unitOfWork, IMapper mapper, IValidator<CustomerDto> validator)
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
                if (!validationResult.IsValid) return validationResult.ToFailedResponse<bool>();

                var customer = _mapper.Map<Customer>(customerDto);
                customer.Id = null; //El Id lo genera la base de datos (columna identity), forzamos a null para no tener problemas al insertar un nuevo registro.
                response.Data = await _unitOfWork._customersUoW.Add(customer);
                var result = await _unitOfWork.SaveChangesAsync();
                if (response.Data && result>0) response.IsSuccess = true;
                else return Response<bool>.Fail("Customer could not be added.");
            }
            catch (Exception ex) { return Response<bool>.Fail(ex.InnerException?.Message ?? ex.Message); }

            return response;
        }

        public async Task<Response<bool>> DeleteAsync(int id)
        {
            var response = new Response<bool>();
            try
            {
                response.Data = await _unitOfWork._customersUoW.Delete(id);
                var result = await _unitOfWork.SaveChangesAsync();
                if (response.Data && result>0) response.IsSuccess = true;
                else return Response<bool>.NotFound($"Customer with ID {id} not found.");
            }
            catch (Exception ex) { return Response<bool>.Fail(ex.InnerException?.Message ?? ex.Message); }

            return response;
        }

        public async Task<Response<IEnumerable<CustomerDto>>> GetAllAsync()
        {
            try
            {
                var customers = await _unitOfWork._customersUoW.GetAllAsync();
                return Response<IEnumerable<CustomerDto>>.Success(_mapper.Map<IEnumerable<CustomerDto>>(customers));
            }
            catch (Exception ex) { return Response<IEnumerable<CustomerDto>>.Fail(ex.InnerException?.Message ?? ex.Message); }
        }

        public async Task<Response<CustomerDto?>> GetByIdAsync(int id)
        {
            try
            {
                var customer = await _unitOfWork._customersUoW.GetByIdAsync(id);
                if (customer == null) return Response<CustomerDto?>.NotFound($"Customer with ID {id} not found.");

                return Response<CustomerDto?>.Success(_mapper.Map<CustomerDto?>(customer));
            }
            catch (Exception ex) { return Response<CustomerDto?>.Fail(ex.InnerException?.Message ?? ex.Message); }
        }

        public async Task<Response<bool>> UpdateAsync(int id, CustomerDto customerDto, CancellationToken cancellationToken)
        {
            var response = new Response<bool>();
            try
            {
                //El DTO de entrada se valida igual que en el alta: es la misma forma y las mismas reglas.
                var validationResult = await _validator.ValidateAsync(customerDto, cancellationToken);
                if (!validationResult.IsValid) return validationResult.ToFailedResponse<bool>();

                var existingCustomer = await _unitOfWork._customersUoW.GetByIdAsync(id);
                if (existingCustomer == null) return Response<bool>.NotFound($"Customer with ID {id} not found.");
                ManualMappingCustomer.MapInto(existingCustomer, customerDto);
                response.Data = await _unitOfWork._customersUoW.Update(existingCustomer);
                var result = await _unitOfWork.SaveChangesAsync();
                if (response.Data && result>0) response.IsSuccess = true;
                else return Response<bool>.Fail($"Customer with ID {id} could not be updated.");
            }
            catch (Exception ex) { return Response<bool>.Fail(ex.InnerException?.Message ?? ex.Message); }
            return response;
        }
    
    }
}


