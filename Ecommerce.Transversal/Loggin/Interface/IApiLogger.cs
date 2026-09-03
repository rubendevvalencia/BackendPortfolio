using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Transversal.Loggin.Interface
{
    public interface IApiLogger<T>
    {
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, params object[] args);
        void LogError(Exception ex,  string message, params object[] args);
        void LogDebug(string message, params object[] args);
    }
}
