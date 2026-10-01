namespace RegistroPerf.Model;

public class ApiOptions
{
    public const string SectionName = "ApiConf";
    public string BaseUrl { get; set; } = string.Empty;
    public string SignUpPath { get; set; } = string.Empty;
    public string SignInPath { get; set; } = string.Empty;

    // Solo lectura: el binder de configuración las ignora, se calculan a partir de las claves de arriba.
    public string SignUpUrl => BuildUrl(SignUpPath);
    public string SignInUrl => BuildUrl(SignInPath);

    private string BuildUrl(string path)
    {
        string baseUrl = BaseUrl.TrimEnd('/');
        string cleanPath = path.TrimStart('/');
        return $"{baseUrl}/{cleanPath}";
    }
}
