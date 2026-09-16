namespace Ecommerce.Application.Common.Interface
{
    //Marca las peticiones que valida ValidationBehaviour en el pipeline de MediatR.
    //Sin esta marca el behaviour se aplicaria a TODAS las peticiones, v3 incluida: v3 valida dentro del handler
    //y devuelve Response.Invalid, asi que el behaviour lanzaria la excepcion antes y cambiaria el contrato de v3.
    //La DI salta un behaviour generico cuando la peticion no cumple su restriccion, por eso basta con la marca.
    public interface IValidatableRequest
    {
    }
}
