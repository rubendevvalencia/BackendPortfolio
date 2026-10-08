using AutoMapper;
using Ecommerce.Application.Mapping;
using Ecommerce.Application.Validator;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using FluentValidation;
using MediatR;

namespace Ecommerce.Application.Feature.Customers.Commands.UpdateCustomer
{
    public class UpdateCustomerCommandHandle : IRequestHandler<UpdateCustomerCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateCustomerCommand> _validator;

        public UpdateCustomerCommandHandle(IUnitOfWork unitOfWork, IMapper mapper, IValidator<UpdateCustomerCommand> validator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<Response<bool>> Handle(UpdateCustomerCommand request, CancellationToken cancellationToken)
        {
             var response = new Response<bool>();
           
            //El DTO de entrada se valida igual que en el alta: es la misma forma y las mismas reglas.
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid) return validationResult.ToFailedResponse<bool>();

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


