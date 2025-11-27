using System.Net.Http.Json;
using System.Threading.Tasks;
using FocusDeck.Domain.Entities.Context;
using Xunit;

namespace FocusDeck.Server.Tests.Controllers.V1.Context
{
    public class ContextControllerTests : IClassFixture<FocusDeckWebApplicationFactory>
    {
        private readonly FocusDeckWebApplicationFactory _factory;

        public ContextControllerTests(FocusDeckWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Post_Snapshots_ShouldReturnOk()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsync("/api/v1/context/snapshots", null);

            // Assert
            response.EnsureSuccessStatusCode();
            var snapshot = await response.Content.ReadFromJsonAsync<ContextSnapshot>();
            Assert.NotNull(snapshot);
        }
    }
}
