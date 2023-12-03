using NUnit.Framework;
using SonarQubeWorker.Mapper;
using SonarQubeWorker.Interface;
using Moq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;


namespace SonarQubeWorker.Tests
{
    [TestFixture]
    public class MapperTests
    {
        private IMapper _mapper;
        private Mock<ILogger> _mockLogger;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger>();
            _mapper = new Mapper.Mapper(_mockLogger.Object);
        }

        [Test]
        public async Task MapToResults_CorrectlyMapsValidJson()
        {
            // Arrange
            string validJson = @"
{
    ""component"": {
        ""id"": ""AYwBnFz-YvxQjtTbAh4D"",
        ""key"": ""circustrein_1699625745679.zip"",
        ""name"": ""circustrein_1699625745679.zip"",
        ""qualifier"": ""TRK"",
        ""measures"": [
            {
                ""metric"": ""coverage"",
                ""value"": ""0.0"",
                ""bestValue"": false
            },
            {
                ""metric"": ""reliability_rating"",
                ""value"": ""1.0"",
                ""bestValue"": true
            },
            {
                ""metric"": ""complexity"",
                ""value"": ""36""
            },
            {
                ""metric"": ""bugs"",
                ""value"": ""0"",
                ""bestValue"": true
            },
            {
                ""metric"": ""code_smells"",
                ""value"": ""9"",
                ""bestValue"": false
            },
            {
                ""metric"": ""security_rating"",
                ""value"": ""1.0"",
                ""bestValue"": true
            },
            {
                ""metric"": ""vulnerabilities"",
                ""value"": ""0"",
                ""bestValue"": true
            },
            {
                ""metric"": ""security_review_rating"",
                ""value"": ""1.0"",
                ""bestValue"": true
            },
            {
                ""metric"": ""security_hotspots"",
                ""value"": ""0"",
                ""bestValue"": true
            },
            {
                ""metric"": ""sqale_rating"",
                ""value"": ""1.0"",
                ""bestValue"": true
            }
        ]
    }
}";

            // Act
            var result = await _mapper.MapToResults(validJson);

            // Assert
            Assert.IsNotNull(result);
            // Assert that each property is mapped correctly
            // For example:
            Assert.AreEqual(0.0, result.Coverage);
            Assert.AreEqual(1.0, result.ScaleRating);
            // ... other assertions for each property
        }

        [Test]
        public void MapToResults_ThrowsExceptionForInvalidJson()
        {
            // Arrange
            string invalidJson = "invalid json";

            // Act & Assert
            Assert.ThrowsAsync<JsonReaderException>(async () => await _mapper.MapToResults(invalidJson));
        }

        [Test]
        public async Task MapToResults_ReturnsNullForNullJson()
        {
            // Arrange
            string nullJson = null;

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _mapper.MapToResults(nullJson));
        }

        // Additional tests for edge cases, different metrics, etc.
    }
}
