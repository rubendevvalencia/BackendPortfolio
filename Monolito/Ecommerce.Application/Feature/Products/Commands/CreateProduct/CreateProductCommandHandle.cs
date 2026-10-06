using AutoMapper;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using MediatR;

namespace Ecommerce.Application.Feature.Products.Commands.CreateProduct
{
    public class CreateProductCommandHandle : IRequestHandler<CreateProductCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        public CreateProductCommandHandle(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }
        public async Task<Response<bool>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            var response = new Response<bool>();
            var product = _mapper.Map<Product>(request);
            product.Id = null; //El Id lo genera la base de datos (columna identity), forzamos a null para no tener problemas al insertar un nuevo registro.
            var exitingProduct = await _unitOfWork._products.CompareInfoInDb(product, cancellationToken);
            if (exitingProduct) return Response<bool>.Fail("Product is already registered", ErrorType.Duplicated);

            //Dos pasos: el repositorio marca la intencion, el UnitOfWork confirma.
            await _unitOfWork._products.AddAsync(product, cancellationToken);
            var result = await _unitOfWork.SaveChangesAsync(cancellationToken);
            response.Data = result > 0 ? true : false;

            if (response.Data) response.IsSuccess = true;
            else return Response<bool>.Fail("Product could not be added.");

            return response;
        }

    }
}


