using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using MediatR;

namespace Ecommerce.Application.Feature.ProductToCustomer
{
    public class ProductToCustomerCommandHandle : IRequestHandler<ProductToCustomerCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        
        public ProductToCustomerCommandHandle(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<bool>> Handle(ProductToCustomerCommand request, CancellationToken cancellationToken)
        {
            var response = new Response<bool>();
        
            var customer = await _unitOfWork._customersUoW.GetByIdAsync(request.CustomerId);
            var product = await _unitOfWork._products.GetByIdAsync(request.ProductId);
            if (customer == null || product == null) return Response<bool>.Fail("Product or Customer not found", ErrorType.NotFound);
            
            // Agregar el producto al cliente
            customer.Products = customer.Products ?? new List<Product>();
            customer.Products.Add(product);
            _unitOfWork._customersUoW.Update(customer);
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            response.Data = result > 0 ? true : false;
            if (response.Data) response.IsSuccess = true;
            else return Response<bool>.Fail("Product could not be added.", ErrorType.InvalidOperation);
            return Response<bool>.Success(true);

        }

    }
}


