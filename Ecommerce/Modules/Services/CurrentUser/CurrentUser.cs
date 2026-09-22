using Ecommerce.Domain.Interface.IAuthIdentification;

namespace Ecommerce.Api.Modules.Services.CurrentUser;

//Implementacion de ICurrentUser en la Api porque es la unica capa que conoce HttpContext.
//El middleware de JWT deja el ClaimsPrincipal en HttpContext.User al validar el token; aqui solo se lee.
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetUserName()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null; //No hay peticion HTTP (migraciones, procesos en segundo plano).

        var identity = httpContext.User.Identity;
        if (identity == null || !identity.IsAuthenticated) return null; //La peticion no trae un token valido.

        return identity.Name; //Claim ClaimTypes.Name, que JwtApplication rellena con el UserName.
    }
}
