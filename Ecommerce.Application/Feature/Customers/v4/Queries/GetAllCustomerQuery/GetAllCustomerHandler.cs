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
        private readonly IDistributedCache _distributedCache;

        public GetAllCustomerHandler(ICustomerReadRepository customerReadRepo, IMapper mapper, IDistributedCache distributedCache)
        {
            _customerReadRepo = customerReadRepo;
            _mapper = mapper;
            _distributedCache = distributedCache;
        }

        public async Task<Response<IEnumerable<CustomerDto>>> Handle(GetAllCustomerQuery request, CancellationToken cancellationToken)
        {

            IEnumerable<object> customers;

            var cacheKey = "getAllCustomers";
            var redisCategories = await _distributedCache.GetAsync(cacheKey);

            if (redisCategories != null) customers = JsonSerializer.Deserialize<IEnumerable<CustomerDto>>(redisCategories);
            else
            {
                customers = await _customerReadRepo.GetAllAsync(cancellationToken);
                if(customers != null)
                {
                    //Configuramos el caché
                    var serializedCategories = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(customers));
                    var options = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromHours(2)) //Tiempo máximo registrado
                        .SetSlidingExpiration(TimeSpan.FromHours(1));  //El tiempo máximo que dura sino se consulta, si se consume no se borra en 1h

                    await _distributedCache.SetAsync(cacheKey, serializedCategories, options, cancellationToken);
                }
            }
            
            return Response<IEnumerable<CustomerDto>>.Success(_mapper.Map<IEnumerable<CustomerDto>>(customers));
        }
    }
}
