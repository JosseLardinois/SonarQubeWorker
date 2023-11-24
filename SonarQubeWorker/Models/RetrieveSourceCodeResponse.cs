namespace Sonarqube_API.Models
{
    public class RetrieveSourceCodeResponse
    {
        public string? Uri { get; set; }
        public string? Name { get; set; }
        public string? ContentType { get; set; }
        public Stream? Content { get; set; }
        public string FilePath { get; set; }
    }
}
