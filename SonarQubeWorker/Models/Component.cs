namespace Sonarqube_API.Models
{
    public class Component
    {
        public string Name { get; set; }
        public List<Measure> Measures { get; set; }
    }
}
