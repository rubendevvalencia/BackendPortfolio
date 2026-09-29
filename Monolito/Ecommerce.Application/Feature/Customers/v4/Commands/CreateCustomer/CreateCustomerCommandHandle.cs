using AutoMapper;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using MediatR;

namespace Ecommerce.Application.Feature.Customers.v4.Commands.CreateCustomer
{
    public class CreateCustomerCommandHandle : IRequestHandler<CreateCustomerCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        //Diferencia con v3: sin IValidator. La validacion la hace ValidationBehaviour antes de llegar aqui.
        public CreateCustomerCommandHandle(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Response<bool>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
        {
            var response = new Response<bool>();

            var customer = _mapper.Map<Customer>(request);
            customer.Id = null; //El Id lo genera la base de datos (columna identity), forzamos a null para no tener problemas al insertar un nuevo registro.
            var exitingUser = await _unitOfWork._customersUoW.CompareInfoInDb(customer, cancellationToken);
            if (exitingUser) return Response<bool>.Fail("Customer is already registered", ErrorType.Duplicated);

            //Dos pasos: el repositorio marca la intencion, el UnitOfWork confirma.
            await _unitOfWork._customersUoW.AddAsync(customer, cancellationToken);
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            response.Data = result > 0 ? true : false;

            if (response.Data) response.IsSuccess = true;
            else return Response<bool>.Fail("Customer could not be added.");

            return response;
        }
    }
}
