using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Feature.Customers.Queries.GetAllCustomerQuery
{
    public class GetAllCustomerHandler : IRequestHandler<GetAllCustomerQuery, Response<IEnumerable<CustomerDto>>>
    {
        private readonly IMapper _mapper;
        private readonly ICustomerReadRepository _customerReadRepo;
        public GetAllCustomerHandler(ICustomerReadRepository customerReadRepo, IMapper mapper)
        {
            _customerReadRepo = customerReadRepo;
            _mapper = mapper;
        }
        public async Task<Response<IEnumerable<CustomerDto>>> Handle(GetAllCustomerQuery request, CancellationToken cancellationToken)
        {
            var customers = await _customerReadRepo.GetAllAsync();
            return Response<IEnumerable<CustomerDto>>.Success(_mapper.Map<IEnumerable<CustomerDto>>(customers));
        }
    }
}
