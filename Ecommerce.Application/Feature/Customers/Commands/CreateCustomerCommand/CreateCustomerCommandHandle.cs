using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Application.Validator;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Feature.Customers.Commands.CreateCustomerCommand
{
    public class CreateCustomerCommandHandle : IRequestHandler<CreateCustomerCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateCustomerCommand> _validator;

        public CreateCustomerCommandHandle(IUnitOfWork unitOfWork, IMapper mapper, IValidator<CreateCustomerCommand> validator)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<Response<bool>> Handle(CreateCustomerCommand request, CancellationToken cancellationToken)
        {
            var response = new Response<bool>();

            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid) return validationResult.ToFailedResponse<bool>();
            var customer = _mapper.Map<Customer>(request);
            customer.Id = null; //El Id lo genera la base de datos (columna identity), forzamos a null para no tener problemas al insertar un nuevo registro.
            var exitingUser = await _unitOfWork._customersUoW.CompareInfoInDb(customer, cancellationToken);
            if (exitingUser != null) return Response<bool>.Fail("Customer are registered");

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
