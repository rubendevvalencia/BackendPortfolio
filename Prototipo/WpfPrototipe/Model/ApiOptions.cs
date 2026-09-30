namespace RegistroPerf.Model;

public class ApiOptions
{
    public const string SectionName = "ApiConf";
    public string BaseUrl { get; set; } = string.Empty;
    public string SignUpPath { get; set; } = string.Empty;
}
