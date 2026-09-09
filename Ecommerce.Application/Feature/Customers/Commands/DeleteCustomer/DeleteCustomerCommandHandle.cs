using AutoMapper;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using FluentValidation;
using MediatR;

namespace Ecommerce.Application.Feature.Customers.Commands.DeleteCustomer
{
    public class DeleteCustomerCommandHandle : IRequestHandler<DeleteCustomerCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<DeleteCustomerCommand> _validator;
        public DeleteCustomerCommandHandle(IUnitOfWork unitOfWork, IMapper mapper, IValidator<DeleteCustomerCommand> validator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<Response<bool>> Handle(DeleteCustomerCommand command, CancellationToken cancellationToken)
        {
            var response = new Response<bool>();
           
            //Comprobar si existe es responsabilidad del caso de uso, no del repositorio:
            //asi el "no existe" (404) queda separado del "no se pudo borrar" (500).
            var customer = await _unitOfWork._customersUoW.GetByIdAsync(command.Id);
            if (customer is null) return Response<bool>.NotFound($"Customer with ID {command.Id} not found.");

            _unitOfWork._customersUoW.Delete(customer); //No se pone await porque es void, lo ejecuta el SavechangeAsync
            var result = await _unitOfWork.SaveChangesAsync();
            response.Data = result > 0 ? true : false;

            if (response.Data) response.IsSuccess = true;
            else return Response<bool>.Fail($"Customer with ID {command.Id} could not be deleted.");

            return response;
        }

       

    }
}


