using AutoMapper;
using Ecommerce.Application.Common.Interface;
using Ecommerce.Domain.Entities.Jwt;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Feature.Users.Commands.SignUp
{
    public class SignUpCommandHandle : IRequestHandler<SignUpCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<SignUpCommandHandle> _logger;

        public SignUpCommandHandle(IUnitOfWork unitOfWork, IMapper mapper, ILogger<SignUpCommandHandle> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Response<bool>> Handle(SignUpCommand request, CancellationToken cancellationToken)
        {
            var exitingUser = await _unitOfWork._user.GetByEmailAsync(request.Email);
            var exitingUserName = await _unitOfWork._user.GetByUserNameAsync(request.UserName);
            if(exitingUser != null || exitingUserName != null)
            {
                //No se loguea aqui: LoggingBehaviour ya deja el Warning de Duplicated, sin el email.
                return Response<bool>.Fail("User already exists", ErrorType.Duplicated);
            }

            var user = _mapper.Map<User>(request);

            //Dos pasos: el repositorio registra el alta y el caso de uso confirma, una sola vez.
            await _unitOfWork._user.CreateUserAsync(user);

            var filasEscritas = await _unitOfWork.SaveChangesAsync(cancellationToken);
            var usuarioCreado = filasEscritas > 0;
            if(!usuarioCreado) return Response<bool>.Fail("Failed to create user", ErrorType.Unexpected);
            return Response<bool>.Success(true);
        }
    }
}


