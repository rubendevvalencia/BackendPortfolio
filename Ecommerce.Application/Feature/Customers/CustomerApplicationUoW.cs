using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Application.Interface;
using Ecommerce.Application.Mapping;
using Ecommerce.Application.Validator;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using FluentValidation;

namespace Ecommerce.Application.Feature.Customers
{
    public class CustomerApplicationUoW : ICustomerApplicationUoW
    {
        //La capa Application orquesta el caso de uso y decide el limite transaccional:
        //registra los cambios en los repositorios y confirma una sola vez con SaveChangesAsync.
        //Se depende de la abstraccion IValidator<CustomerDto>, no de la clase concreta:
        //el servicio no conoce la implementacion y en los tests se puede sustituir por un doble.
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CustomerDto> _validator; //Se inyecta el validador de CustomerDto la abstracción en vez de la implementación, que es el que contiene las reglas de validación para la entidad Customer.

        public CustomerApplicationUoW(IUnitOfWork unitOfWork, IMapper mapper, IValidator<CustomerDto> validator)
        {
            _unitOfWork = unitOfWork;
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
            var exitingUser = await _unitOfWork._customersUoW.CompareInfoInDb(customer, cancellationToken);
            if(exitingUser != null) return Response<bool>.Fail("Customer are registered");

            //Dos pasos: el repositorio marca la intencion, el UnitOfWork confirma.
            await _unitOfWork._customersUoW.AddAsync(customer, cancellationToken);
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            response.Data = result > 0 ? true : false;

            if (response.Data) response.IsSuccess = true;
            else return Response<bool>.Fail("Customer could not be added.");
        
            return response;
        }

        public async Task<Response<bool>> DeleteAsync(int id)
        {
            var response = new Response<bool>();
           
            //Comprobar si existe es responsabilidad del caso de uso, no del repositorio:
            //asi el "no existe" (404) queda separado del "no se pudo borrar" (500).
            var customer = await _unitOfWork._customersUoW.GetByIdAsync(id);
            if (customer is null) return Response<bool>.NotFound($"Customer with ID {id} not found.");

            _unitOfWork._customersUoW.Delete(customer); //No se pone await porque es void, lo ejecuta el SavechangeAsync
            var result = await _unitOfWork.SaveChangesAsync();
            response.Data = result > 0 ? true : false;

            if (response.Data) response.IsSuccess = true;
            else return Response<bool>.Fail($"Customer with ID {id} could not be deleted.");

            return response;
        }

        public async Task<Response<IEnumerable<CustomerDto>>> GetAllAsync()
        {
            var customers = await _unitOfWork._customersUoW.GetAllAsync();
                return Response<IEnumerable<CustomerDto>>.Success(_mapper.Map<IEnumerable<CustomerDto>>(customers));
        }

        public async Task<Response<CustomerDto?>> GetByIdAsync(int id)
        {
           var customer = await _unitOfWork._customersUoW.GetByIdAsync(id);
            if (customer == null) return Response<CustomerDto?>.NotFound($"Customer with ID {id} not found.");

            return Response<CustomerDto?>.Success(_mapper.Map<CustomerDto?>(customer));
           
        }

        public async Task<Response<bool>> UpdateAsync(int id, CustomerDto customerDto, CancellationToken cancellationToken)
        {
            var response = new Response<bool>();
           
            //El DTO de entrada se valida igual que en el alta: es la misma forma y las mismas reglas.
            var validationResult = await _validator.ValidateAsync(customerDto, cancellationToken);
            if (!validationResult.IsValid) return validationResult.ToFailedResponse<bool>();

            var existingCustomer = await _unitOfWork._customersUoW.GetByIdAsync(id, cancellationToken);
            if (existingCustomer == null) return Response<bool>.NotFound($"Customer with ID {id} not found.");

            ManualMappingCustomer.MapInto(existingCustomer, customerDto);

            //La entidad viene trackeada desde GetByIdAsync, asi que Update() no hace nada:
            //se deja explicito para que el caso de uso declare su intencion de modificar.
            _unitOfWork._customersUoW.Update(existingCustomer); //No se pone await porque es void, lo ejecuta el SavechangeAsync
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            response.Data = result > 0 ? true : false;

            if (response.Data) response.IsSuccess = true;
            else return Response<bool>.Fail($"Customer with ID {id} could not be updated.");
           
            return response;
        }
    }
}
