using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ecommerce.Infrastructure.Data.Cache
{
    public static class CacheConfiguration
    {
        //Devuelve [0] = AbsoluteExpiration y [1] = SlidingExpiration, ya como TimeSpan.
        //En appsettings.json las caducidades se escriben en formato "hh:mm:ss", asi que aqui
        //solo hay que reconocerlas: quien llama recibe el TimeSpan hecho y no necesita
        //TimeSpan.FromHours ni saber en que unidad estaba escrito el valor.
        public static TimeSpan[] Configuration(eCacheKey key, IConfiguration configuration)
        {
            string? absoluteTime;
            string? slidingTime;

            if(key == eCacheKey.CustomerAll)
            {
                absoluteTime = configuration["Cache:Policies:CustomerAll:AbsoluteExpiration"];
                slidingTime = configuration["Cache:Policies:CustomerAll:SlidingExpiration"];
            }
            else
            {
                absoluteTime = configuration["Cache:Default:AbsoluteExpiration"];
                slidingTime = configuration["Cache:Default:SlidingExpiration"];
            }

            //InvariantCulture a proposito: el valor viene de un archivo de configuracion, no del
            //usuario, y no debe depender de la cultura de la maquina donde se despliegue.
            //TryParseExact y no TryParse: TimeSpan lee un entero suelto como DIAS ("30" => 30 dias),
            //asi que un valor mal escrito pasaria la validacion y daria una caducidad absurda.
            //Exigiendo "hh:mm:ss" ese error se convierte en un fallo al arrancar.
            var absoluteOk = TimeSpan.TryParseExact(absoluteTime, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out var absoluteExpiration);

            if (!absoluteOk) throw new InvalidOperationException($"AbsoluteExpiration de '{key}' no es un TimeSpan valido (formato esperado \"hh:mm:ss\").");

            var slidingOk = TimeSpan.TryParseExact(slidingTime, @"hh\:mm\:ss", CultureInfo.InvariantCulture, out var slidingExpiration);

            if (!slidingOk) throw new InvalidOperationException($"SlidingExpiration de '{key}' no es un TimeSpan valido (formato esperado \"hh:mm:ss\").");

            TimeSpan[] conf = new TimeSpan[2];
            conf[0] = absoluteExpiration;
            conf[1] = slidingExpiration;

            return conf;
        }
    }
}
