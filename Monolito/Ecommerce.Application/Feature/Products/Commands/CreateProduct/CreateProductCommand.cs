using Ecommerce.Application.Common.Interface;
using Ecommerce.Application.Dto;
using Ecommerce.Domain.Enum;
using Ecommerce.Transversal.Common;
using FluentValidation;
using MediatR;

namespace Ecommerce.Application.Feature.Products.Commands.CreateProduct
{
    public class CreateProductCommand : IRequest<Response<bool>>, IValidatableRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; } = 0;
        public int? StockQuantity { get; set; } = 0;
        public int? Category { get; set; }
    }
}


