using Ecommerce.Application.Mapping;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using MediatR;

namespace Ecommerce.Application.Feature.Customers.v4.Commands.UpdateCustomer
{
    public class UpdateCustomerCommandHandle : IRequestHandler<UpdateCustomerCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        //Diferencia con v3: sin IValidator. La validacion la hace ValidationBehaviour antes de llegar aqui.
        public UpdateCustomerCommandHandle(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<bool>> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
        {
            var response = new Response<bool>();

            var existingCustomer = await _unitOfWork._customersUoW.GetByIdAsync(request.Id, cancellationToken);
            if (existingCustomer == null) return Response<bool>.NotFound($"Customer with ID {request.Id} not found.");

            ManualMappingCustomer.MapInto(existingCustomer, request);

            //La entidad viene trackeada desde GetByIdAsync, asi que Update() no hace nada:
            //se deja explicito para que el caso de uso declare su intencion de modificar.
            _unitOfWork._customersUoW.Update(existingCustomer); //No se pone await porque es void, lo ejecuta el SavechangeAsync
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            response.Data = result > 0 ? true : false;

            if (response.Data) response.IsSuccess = true;
            else return Response<bool>.Fail($"Customer with ID {request.Id} could not be updated.");

            return response;
        }
    }
}
