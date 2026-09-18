using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace Ecommerce.Application.Feature.Customers.v4.Queries.GetAllCustomerQuery
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
            var customers = await _customerReadRepo.GetAllAsync(cancellationToken);
            return Response<IEnumerable<CustomerDto>>.Success(_mapper.Map<IEnumerable<CustomerDto>>(customers));
        }
    }
}
