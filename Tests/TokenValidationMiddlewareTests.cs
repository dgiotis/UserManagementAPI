using Xunit;
using Microsoft.AspNetCore.Http;
using UserManagementAPI.Middleware;
using Moq;

namespace UserManagementAPI.Tests
{
    public class TokenValidationMiddlewareTests
    {
        private readonly TokenValidationMiddleware _middleware;
        private readonly Mock<ILogger<TokenValidationMiddleware>> _mockLogger;

        public TokenValidationMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<TokenValidationMiddleware>>();
            
            // Create a mock next delegate that completes successfully
            RequestDelegate next = async (HttpContext context) =>
            {
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("OK");
            };

            _middleware = new TokenValidationMiddleware(next, _mockLogger.Object);
        }

        [Fact]
        public async Task InvokeAsync_WithValidBearerToken_CallsNextMiddleware()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/users";
            context.Request.Headers.Add("Authorization", "Bearer test-token-123");

            var nextCalled = false;
            RequestDelegate next = async (HttpContext ctx) =>
            {
                nextCalled = true;
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("OK");
            };

            var middleware = new TokenValidationMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal(200, context.Response.StatusCode);
            Assert.NotNull(context.Items["Token"]);
            Assert.Equal("test-token-123", context.Items["Token"]);
        }

        [Fact]
        public async Task InvokeAsync_WithMissingAuthorizationHeader_Returns401()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/users";
            context.Response.Body = new MemoryStream();

            RequestDelegate next = async (HttpContext ctx) =>
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("OK");
            };

            var middleware = new TokenValidationMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(401, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_WithInvalidBearerToken_Returns401()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/users";
            context.Request.Headers.Add("Authorization", "Bearer invalid-token");
            context.Response.Body = new MemoryStream();

            RequestDelegate next = async (HttpContext ctx) =>
            {
                ctx.Response.StatusCode = 200;
            };

            var middleware = new TokenValidationMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(401, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithInvalidAuthorizationFormat_Returns401()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/users";
            context.Request.Headers.Add("Authorization", "Basic sometoken"); // Wrong scheme
            context.Response.Body = new MemoryStream();

            RequestDelegate next = async (HttpContext ctx) =>
            {
                ctx.Response.StatusCode = 200;
            };

            var middleware = new TokenValidationMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(401, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithSwaggerPath_SkipsTokenValidation()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/swagger/ui/index.html";
            // No Authorization header added

            var nextCalled = false;
            RequestDelegate next = async (HttpContext ctx) =>
            {
                nextCalled = true;
                ctx.Response.StatusCode = 200;
            };

            var middleware = new TokenValidationMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithLoginPath_SkipsTokenValidation()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/auth/login";
            // No Authorization header added

            var nextCalled = false;
            RequestDelegate next = async (HttpContext ctx) =>
            {
                nextCalled = true;
                ctx.Response.StatusCode = 200;
            };

            var middleware = new TokenValidationMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithEmptyToken_Returns401()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/users";
            context.Request.Headers.Add("Authorization", "Bearer   "); // Only whitespace
            context.Response.Body = new MemoryStream();

            RequestDelegate next = async (HttpContext ctx) =>
            {
                ctx.Response.StatusCode = 200;
            };

            var middleware = new TokenValidationMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(401, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithValidToken_StoresTokenInItems()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/users";
            context.Request.Headers.Add("Authorization", "Bearer demo-token-456");

            RequestDelegate next = async (HttpContext ctx) =>
            {
                ctx.Response.StatusCode = 200;
            };

            var middleware = new TokenValidationMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(context.Items.ContainsKey("Token"));
            Assert.Equal("demo-token-456", context.Items["Token"]);
        }

        [Fact]
        public async Task InvokeAsync_ResponseIsJsonContentType()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Path = "/api/users";
            context.Response.Body = new MemoryStream();

            RequestDelegate next = async (HttpContext ctx) =>
            {
                ctx.Response.StatusCode = 200;
            };

            var middleware = new TokenValidationMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal("application/json", context.Response.ContentType);
        }
    }
}
