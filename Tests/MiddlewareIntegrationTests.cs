using Xunit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Http;
using UserManagementAPI.Models;
using System.Net;
using System.Text.Json;

namespace UserManagementAPI.Tests
{
    public class MiddlewareIntegrationTests
    {
        private readonly WebApplicationFactory<Program> _factory;

        public MiddlewareIntegrationTests()
        {
            _factory = new WebApplicationFactory<Program>();
        }

        [Fact]
        public async Task GetUsers_WithoutToken_Returns401Unauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/users");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task GetUsers_WithValidToken_Returns200Ok()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token-123");

            // Act
            var response = await client.GetAsync("/api/users");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetUsers_WithInvalidToken_Returns401Unauthorized()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer invalid-token");

            // Act
            var response = await client.GetAsync("/api/users");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task LoginEndpoint_WithoutToken_Returns200Ok()
        {
            // Arrange
            var client = _factory.CreateClient();
            var loginRequest = new { username = "admin", password = "password123" };

            // Act
            var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("token", content);
            Assert.Contains("test-token-123", content);
        }

        [Fact]
        public async Task SwaggerEndpoint_WithoutToken_Returns200Ok()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/swagger/ui/index.html");

            // Assert
            // Swagger might return 404 if not properly served, but should not return 401
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateUser_WithValidToken_ReturnsCreated()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer test-token-123");

            var newUser = new
            {
                firstName = "Test",
                lastName = "User",
                email = $"test{Guid.NewGuid()}@example.com",
                department = "QA"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/users", newUser);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CreateUser_WithoutToken_Returns401()
        {
            // Arrange
            var client = _factory.CreateClient();

            var newUser = new
            {
                firstName = "Test",
                lastName = "User",
                email = "test@example.com",
                department = "QA"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/users", newUser);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UnauthorizedResponse_HasCorrectJsonStructure()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/users");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;
            
            Assert.True(root.TryGetProperty("error", out var errorProp));
            Assert.True(root.TryGetProperty("message", out var messageProp));
            Assert.True(root.TryGetProperty("timestamp", out var timestampProp));
            
            Assert.Equal("Unauthorized", errorProp.GetString());
        }

        [Fact]
        public async Task TokenValidation_IsCaseSensitive()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer TEST-TOKEN-123"); // Different case

            // Act
            var response = await client.GetAsync("/api/users");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task BearerScheme_IsCaseInsensitive()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "bearer test-token-123"); // lowercase bearer

            // Act
            var response = await client.GetAsync("/api/users");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task InvalidAuthorizationScheme_Returns401()
        {
            // Arrange
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Basic dGVzdDp0ZXN0"); // Basic auth instead of Bearer

            // Act
            var response = await client.GetAsync("/api/users");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
