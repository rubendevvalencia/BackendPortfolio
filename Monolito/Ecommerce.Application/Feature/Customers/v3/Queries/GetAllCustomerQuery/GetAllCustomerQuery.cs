using Ecommerce.Application.Dto;
using Ecommerce.Transversal.Common;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Feature.Customers.Queries.GetAllCustomerQuery
{
    public class GetAllCustomerQuery : IRequest<Response<IEnumerable<CustomerDto>>>
    {
        //Esto solo es para datos que vienen de fuera, por eso, se deja vacía
    }
}
