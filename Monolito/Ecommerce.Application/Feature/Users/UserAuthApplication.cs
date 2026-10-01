using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Application.Interface.Jwt;
using Ecommerce.Application.Validator;
using Ecommerce.Domain.Entities.Jwt;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using Ecommerce.Transversal.Loggin.Interface;
using FluentValidation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.Feature.Users
{
    public class UserAuthApplication : IUserAuthApplication
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<SignUpDto> _validatorSignUp; 
        private readonly IValidator<SignInDto> _validatorSignIn;
        private readonly IJwtApplication _genJwt;
        private readonly ILogger<UserAuthApplication> _logger; //Le agregamos un logger para poder registrar eventos y errores en la clase AuthApplication.
        public UserAuthApplication(IUnitOfWork unitOfWork, IMapper mapper, IValidator<SignUpDto> validatorSignUp, IValidator<SignInDto> validatorSignIn, IJwtApplication genJwt, ILogger<UserAuthApplication> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _validatorSignUp = validatorSignUp;
            _validatorSignIn = validatorSignIn;
            _genJwt = genJwt;
            _logger = logger;
        }

        public async Task<Response<bool>> SignUpAsync(SignUpDto entity)
        {
            var response = new Response<bool>();
            try
            {
                var validationResult = await _validatorSignUp.ValidateAsync(entity);
                //Devuelve el detalle por propiedad ("Email" -> ["Invalid email format."]) en vez de un
                //mensaje generico, igual que hace CustomerApplication. La traduccion vive en un unico
                //sitio: ValidationResultExtensions.ToFailedResponse<T>().
                if (!validationResult.IsValid) return validationResult.ToFailedResponse<bool>();

                var exitingUser = await _unitOfWork._user.GetByEmailAsync(entity.Email);
                var exitingUserName = await _unitOfWork._user.GetByUserNameAsync(entity.UserName);
                if(exitingUser != null || exitingUserName != null)
                {
                    response.IsSuccess = false;
                    response.Message = "User already exists";
                    response.ErrorType = ErrorType.Validation;
                    _logger.LogWarning("User already exists: {Email}", entity.Email);
                    return response;
                }

                var user = _mapper.Map<User>(entity);

                //Dos pasos: el repositorio registra el alta y el caso de uso confirma, una sola vez.
                await _unitOfWork._user.CreateUserAsync(user);

                var filasEscritas = await _unitOfWork.SaveChangesAsync();
                var usuarioCreado = filasEscritas > 0;
                response.Data = usuarioCreado;

                if(!response.Data)
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to create user";
                    response.ErrorType = ErrorType.Unexpected;
                    _logger.LogWarning("Failed to create user: {Email}", entity.Email);
                }
                else
                {
                    response.IsSuccess = true;
                    response.Message = "User created successfully";
                }
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = "Unhandle exception";
                response.ErrorType = ErrorType.Unexpected;
                _logger.LogError(ex, "An error occurred while signing up user: {Email}", entity.Email);
            }

            return response;
        }

        public async Task<Response<TokenDto>> SingInAsync(SignInDto entity)
        {
            var response = new Response<TokenDto>();
            try
            {
                var validationResult = await _validatorSignIn.ValidateAsync(entity);
                if (!validationResult.IsValid) return validationResult.ToFailedResponse<TokenDto>();

                var user = await _unitOfWork._user.GetByEmailAsync(entity.Email);

                //Mismo Message y mismo ErrorType tanto si el email no existe como si la contraseña es incorrecta:
                //distinguirlos permite enumerar emails registrados probando contraseñas al azar.
                if (user == null)
                {
                    response.IsSuccess = false;
                    response.Message = "Invalid credentials";
                    response.ErrorType = ErrorType.Unauthorized;
                    return response;
                }

                var validPass = _unitOfWork._user.CheckPass(user, entity.Password);
                if (!validPass)
                {
                    response.IsSuccess = false;
                    response.Message = "Invalid credentials";
                    response.ErrorType = ErrorType.Unauthorized;
                    return response;
                }

                (var token, int expiresIn) = _genJwt.GenerateToken(user);
                response.Data = new TokenDto
                {
                    AccessToken = token,
                    ExpiresIn = expiresIn,
                    TokenType = "Bearer"
                };

                response.IsSuccess = true;
                response.Message = "User signed in successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message =  "Unhandle exception";
                response.ErrorType = ErrorType.Unexpected;
                _logger.LogError(ex, "An error occurred while signing in user: {Email}", entity.Email);
            }

            return response;
        }
    }
}
