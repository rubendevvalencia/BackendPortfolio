using Ecommerce.Application.Dto;
using Ecommerce.Transversal.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Feature.Customers.Queries.GetCustomerQuery
{
    public class GetCustomerQuery : IRequest<Response<CustomerDto>>
    {
        public int Id { get; set; }
    }
}
