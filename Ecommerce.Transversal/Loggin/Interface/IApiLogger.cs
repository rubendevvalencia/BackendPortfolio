using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Transversal.Loggin.Interface
{
    //El enfoque manual de logging, conservado como referencia: es el único posible fuera de MediatR.
    //Se llama de forma explícita en el punto donde se toma la decisión, y por eso puede registrar el *porqué*
    //de un fallo esperado —qué regla se incumplió y con qué dato—, algo que un interceptor de pipeline no
    //puede inferir desde fuera, porque solo ve el request de entrada y el response de salida.
    //Su razón de ser como abstracción propia es que el consumidor no referencie Microsoft.Extensions.Logging.
    //No está registrado ahora mismo: hoy v3 se cubre con LoggingBehaviour y auth usa ILogger<T> directo.
    //Ver Transversal/ConfigureServices para reactivarlo, y "Logging" en el README para el porqué.
    public interface IApiLogger<T>
    {
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, params object[] args);
        void LogError(Exception ex,  string message, params object[] args);
        void LogDebug(string message, params object[] args);
    }
}
