using System.Net;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ecommerce.Api.Modules.HealthCheck
{
    public static class HealthHtmlUi
    {
        public static string? htmlFormat(HealthReport report)
        {
            var rows = new StringBuilder();

            foreach (var entry in report.Entries)
            {
                var name = WebUtility.HtmlEncode(entry.Key);
                var status = entry.Value.Status.ToString();
                var description = WebUtility.HtmlEncode(entry.Value.Description ?? "");
                var duration = entry.Value.Duration.TotalMilliseconds.ToString("0.000");

                rows.Append($"<tr><td>{name}</td><td class=\"{status}\">{status}</td><td>{duration} ms</td><td>{description}</td></tr>");
            }

            return $$"""
                <!DOCTYPE html>
                <html lang="es">
                <head>
                    <meta charset="utf-8">
                    <title>Health Ecommerce API</title>
                    <style>
                        body { font-family: sans-serif; margin: 2rem; }
                        table { border-collapse: collapse; }
                        th, td { border: 1px solid #ccc; padding: .5rem 1rem; text-align: left; }
                        .Healthy { color: green; } .Degraded { color: orange; } .Unhealthy { color: red; }
                    </style>
                </head>
                <body>
                    <h1>Estado: <span class="{{report.Status}}">{{report.Status}}</span></h1>
                    <p>Duración total: {{report.TotalDuration.TotalMilliseconds:0.000}} ms</p>
                    <table>
                        <tr><th>Check</th><th>Estado</th><th>Duración</th><th>Descripción</th></tr>
                        {{rows}}
                    </table>
                </body>
                </html>
                """;
        }
    }   
}


