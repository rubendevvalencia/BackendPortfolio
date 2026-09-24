namespace Ecommerce.Transversal.Common.Enums
{
    //Clasifica el motivo del fallo para que la capa de presentacion (Api) pueda traducirlo
    //al codigo HTTP correcto sin conocer los detalles del caso de uso.
    public enum ErrorType
    {
        //Nunca empezamos en 0 porque por detrás es un int y si da un error inesperado, devuelve 0 y no puede devolver estado esperado.
        None = 1,        //La operacion fue correcta.
        Validation = 2,  //El DTO recibido no cumple las reglas -> 400 Bad Request.
        NotFound = 3,    //El recurso solicitado no existe -> 404 Not Found.
        Unexpected = 4,   //Excepcion no controlada -> 500 Internal Server Error.
        Duplicated = 5,    //Información duplicada  
        TimeOut = 6

    }
}
