namespace Sonarqube_API.Models
{
    public class SonarQubeResults
    {
        public string Name { get; set; }
        public double ScaleRating { get; set; }
        public double SecurityReviewRating { get; set; }
        public double ReliabilityRating { get; set; }
        public int CodeSmells { get; set; }
        public int Bugs { get; set; }
        public int Vulnerabilities { get; set; }
        public double Coverage { get; set; }
        public double SecurityRating { get; set; }
        public int SecurityHotspots { get; set; }
    }


}
