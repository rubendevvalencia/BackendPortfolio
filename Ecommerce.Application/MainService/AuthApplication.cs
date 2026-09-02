using AutoMapper;
using Ecommerce.Application.Dto;
using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Application.Interface.Jwt;
using Ecommerce.Domain.Entities.Jwt;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.MainService
{
    public class AuthApplication : IAuthApplication
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidator<SignUpDto> _validatorSignUp;
        private readonly IValidator<SignInDto> _validatorSignIn;
        private readonly IJwtApplication _genJwt;
        public AuthApplication(IUnitOfWork unitOfWork, IMapper mapper, IValidator<SignUpDto> validatorSignUp, IValidator<SignInDto> validatorSignIn, IJwtApplication genJwt)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _validatorSignUp = validatorSignUp;
            _validatorSignIn = validatorSignIn;
            _genJwt = genJwt;
        }

        public async Task<Response<bool>> SignUpAsync(SignUpDto entity)
        {
            var response = new Response<bool>();
            try
            {
                var exitingUser = await _unitOfWork._user.GetByEmailAsync(entity.Email);
                if(exitingUser != null)
                {
                    response.IsSuccess = false;
                    response.Message = "User already exists";
                    response.ErrorType = ErrorType.Validation;
                    return response;
                }

                var user = _mapper.Map<User>(entity);
                response.Data = await _unitOfWork._user.CreateUserAsync(user);
                if(!response.Data)
                {
                    response.IsSuccess = false;
                    response.Message = "Failed to create user";
                    response.ErrorType = ErrorType.Unexpected;
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
                response.Message = ex.Message;
                response.ErrorType = ErrorType.Unexpected;
            }

            return response;
        }

        public async Task<Response<TokenDto>> SingInAsync(SignInDto entity)
        {
            var response = new Response<TokenDto>();
            try
            {
                var user = await _unitOfWork._user.GetByEmailAsync(entity.Email);
                if (user == null)
                {
                    response.IsSuccess = false;
                    response.Message = "User not found, email may be incorrect or not registered";
                    response.ErrorType = ErrorType.NotFound;
                    return response;
                }
                
                var validPass = await _unitOfWork._user.CheckPassAsync(user, entity.Password);
                if(!validPass)
                {
                    response.IsSuccess = false;
                    response.Message = "Invalid password";
                    response.ErrorType = ErrorType.Validation;
                    return response;
                }

                var token = _genJwt.GenerateToken(user);
                response.Data = new TokenDto
                {
                    AccessToken = token,
                    ExpiresIn = 3600,
                };

                response.IsSuccess = true;
                response.Message = "User signed in successfully";
            }
            catch (Exception ex)
            {
                response.IsSuccess = false;
                response.Message = ex.Message;
                response.ErrorType = ErrorType.Unexpected;
            }

            return response;
        }
    }
}
