using Ecommerce.Application.Common.Behaviours.Exceptions;
using Ecommerce.Application.Common.Interface;
using Ecommerce.Transversal.Common;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Common.Behaviours
{
    public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IValidatableRequest //Con "IRequest" (sin <T>) no entraba: los comandos implementan IRequest<Response<bool>>, que es otra interfaz
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);
                var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
                var failure = validationResults.Where(e => e.Errors.Any())
                    .SelectMany(e => e.Errors)
                    .Select(e => new BaseError()
                    {
                        PropertyMessage = e.PropertyName,
                        ErrorMessage = e.ErrorMessage
                    }).ToList();

                if(failure.Any())
                {
                    throw new ValidationExceptionCustom(failure);
                }
            }

            return await next();
        }
    }
}
