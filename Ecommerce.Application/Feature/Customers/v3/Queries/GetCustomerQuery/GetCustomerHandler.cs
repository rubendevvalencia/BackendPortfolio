using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Feature.Customers.Queries.GetCustomerQuery
{
    public class GetCustomerHandler : IRequestHandler<GetCustomerQuery, Response<CustomerDto>>
    {
        private readonly ICustomerReadRepository _customerReadRepo;
        private readonly IMapper _mapper;

        public GetCustomerHandler(ICustomerReadRepository customerReadRepo, IMapper mapper)
        {
            _customerReadRepo = customerReadRepo;
            _mapper = mapper;
        }
        public async Task<Response<CustomerDto?>> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
        {
            var customer = await _customerReadRepo.GetByIdAsync(request.Id, cancellationToken);
            if (customer == null) return Response<CustomerDto?>.NotFound($"Customer with ID {request.Id} not found.");
            return Response<CustomerDto?>.Success(_mapper.Map<CustomerDto?>(customer));
        }
    }
}
