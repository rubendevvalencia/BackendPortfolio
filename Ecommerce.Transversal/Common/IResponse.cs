using Ecommerce.Transversal.Common.Enums;

namespace Ecommerce.Transversal.Common
{
    //Cara no genérica de Response<T>. Un interceptor de pipeline recibe la respuesta como un TResponse genérico
    //y no puede preguntar "¿es un Response<T>?" sin conocer el T concreto (bool en un handler, CustomerDto en otro).
    //Con esta interfaz puede leer el resultado sin saberlo. No añade comportamiento: solo hace legible lo que ya existe.
    //Vive en Transversal, junto a Response<T>, porque este proyecto no puede referenciar Application.
    public interface IResponse
    {
        bool IsSuccess { get; }
        string Message { get; }
        ErrorType ErrorType { get; }
    }
}
