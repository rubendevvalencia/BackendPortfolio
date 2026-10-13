using AutoMapper;
using Ecommerce.Application.Dto.Jwt;
using Ecommerce.Application.Interface.Jwt;
using Ecommerce.Domain.Entities.Jwt;
using Ecommerce.Domain.Interface.IRepository;
using Ecommerce.Transversal.Common;
using Ecommerce.Transversal.Common.Enums;
using MediatR;

namespace Ecommerce.Application.Feature.Users.Commands.Delete
{
    public class DeleteCommandHandle : IRequestHandler<DeleteCommand, Response<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        
        public DeleteCommandHandle(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;
        public async Task<Response<bool>> Handle(DeleteCommand request, CancellationToken cancellationToken)
        {
            var response = new Response<bool>();
            var user = await _unitOfWork._user.GetByEmailAsync(request.Email);
            if (user is null) return Response<bool>.Fail("Invalid credentials", ErrorType.Unauthorized);
            var validPass = _unitOfWork._user.CheckPass(user, request.Password!);
            if (!validPass) return Response<bool>.Fail("Invalid credentials", ErrorType.Unauthorized);
            //Esta línea no es necesaria porque en la propia configuración ya filtramos aquellos que no hayan sido eliminados. 
            // builder.HasQueryFilter(u => !u.IsDeleted);
            //if(user.IsDeleted) return Response<bool>.Fail("User was deleted", ErrorType.InvalidOperation); 

            _unitOfWork._user.DeleteUser(user);
            var rowsModified = await _unitOfWork.SaveChangesAsync(cancellationToken);
            var result = await _unitOfWork._user.GetByIdAsync(user.Id);
            if(rowsModified > 0 && result is null) return Response<bool>.Success(true);
            return Response<bool>.Fail("Invalid deleted", ErrorType.Unexpected);
        }
    }
}

