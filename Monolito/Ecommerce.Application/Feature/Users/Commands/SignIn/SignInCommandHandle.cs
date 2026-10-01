using AutoMapper;
using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Application.Interface.Jwt;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Application.Feature.Users.Commands.SignIn
{
    public class SignInCommandHandle : IRequestHandler<SignInCommand, Response<TokenDto>>
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;
        private readonly ILogger<SignInCommandHandle> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IJwtApplication _genJwt;
        public SignInCommandHandle(IMediator mediator, IUnitOfWork unitOfWork, IMapper mapper, ILogger<SignInCommandHandle> logger, IJwtApplication genJwt)
        {
            _mediator = mediator;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _genJwt = genJwt;
        }  
        public async Task<Response<TokenDto>> Handle(SignInCommand request, CancellationToken cancellationToken)
        {
            var response = new Response<TokenDto>();
    
            var user = await _unitOfWork._user.GetByEmailAsync(request.Email);
            if (user == null) return Response<TokenDto>.Fail("Invalid credentials", ErrorType.Unauthorized);
            
            var validPass = _unitOfWork._user.CheckPass(user, request.Password);
            if (!validPass) return Response<TokenDto>.Fail("Invalid credentials", ErrorType.Unauthorized);
            
            (var token, int expiresIn) = _genJwt.GenerateToken(user);
            response.Data = new TokenDto
            {
                AccessToken = token,
                ExpiresIn = expiresIn,
                TokenType = "Bearer"
            };

            return Response<TokenDto>.Success(response.Data);
        }
    }
}


