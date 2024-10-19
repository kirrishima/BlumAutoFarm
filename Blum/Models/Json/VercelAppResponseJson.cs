namespace Blum.Models.Json
{
    public class ErrorResponse
    {
        public VercelAppResponseJson? Error { get; set; } = null;
    }

    public class VercelAppResponseJson
    {
        public string? Code { get; set; } = null;
        public string? Message { get; set; } = null;
    }
}
