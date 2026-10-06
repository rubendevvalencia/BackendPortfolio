using Asp.Versioning;
using Ecommerce.Application.Dto;
using Ecommerce.Application.Feature.Products.Commands.CreateProduct;
using Ecommerce.Transversal.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace Ecommerce.Api.Controllers.Product
{
    [Authorize] //Protege el controlador completo: cualquier endpoint requiere un token JWT válido.
    [EnableRateLimiting("user-limited")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("4.0")]
    [SwaggerTag("Controller for managing product operations.")]
    public class ProductController : ApiResponseControllerBase
    {
        private readonly IMediator _mediator; //Orquesta todo

        public ProductController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("Create")]
        [RequestTimeout("CustomPolicy")]
        [SwaggerOperation(Summary = "Adds a new product.", Description = "Adds a new product to the system.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Product added successfully.", typeof(Response<bool>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The product data is invalid.", typeof(Response<object>))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Product is already registered", typeof(Response<bool>))]
        public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(command, cancellationToken);
            return ToActionResult(response);
        }
        /*
        [HttpPost("Update")]
        [RequestTimeout("CustomPolicy")]
        [SwaggerOperation(Summary = "Updates an existing product using POST.", Description = "Updates the details of an existing product in the system using a POST request.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Product updated successfully.", typeof(Response<bool>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The product data is invalid.", typeof(Response<object>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "The product does not exist.", typeof(Response<bool>))]
        public async Task<IActionResult> Update([FromBody] UpdateProductCommand command, CancellationToken cancellationToken)
        {
            var response = await _mediator.Send(command, cancellationToken);
            return ToActionResult(response);
        }

        [HttpDelete("{id:int}")]
        [RequestTimeout("CustomPolicy")]
        [SwaggerOperation(Summary = "Deletes an existing product.", Description = "Deletes an existing product from the system.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Product deleted successfully.", typeof(Response<bool>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The id is invalid.", typeof(Response<object>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "The product does not exist.", typeof(Response<bool>))]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
        {
            DeleteProductCommand command = new()
            {
                Id = id
            };
            var response = await _mediator.Send(command, cancellationToken);
            return ToActionResult(response);
        }

        [HttpGet("{id:int}")]
        [RequestTimeout("CustomPolicy")]
        [SwaggerOperation(Summary = "Retrieves a product by ID.", Description = "Retrieves the details of a product based on the provided ID.")]
        [SwaggerResponse(StatusCodes.Status200OK, "Product retrieved successfully.", typeof(Response<ProductDto>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "The id is invalid.", typeof(Response<object>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "The product does not exist.", typeof(Response<ProductDto>))]
        public async Task<IActionResult> GetProduct([FromRoute] int id, CancellationToken cancellationToken)
        {
            GetProductQuery query = new()
            {
                Id = id
            };
            var response = await _mediator.Send(query, cancellationToken);
            return ToActionResult(response);
        }

        [HttpGet("All")]
        [SwaggerOperation(
            Summary = "Retrieves all products.", 
            Description = "Retrieves a list of all products in the system.",
            OperationId = "GetAll",
            Tags = new string[] { "GetAll" })]
        [SwaggerResponse(StatusCodes.Status200OK, "Products retrieved successfully.", typeof(Response<IEnumerable<ProductDto>>))]
        public async Task<IActionResult> AllProducts(CancellationToken cancellationToken)
        {
            GetAllProductQuery query = new GetAllProductQuery();
            var response = await _mediator.Send(query, cancellationToken); //TimeOut para cancelación
            return ToActionResult(response);
        }
        */
    }
}


