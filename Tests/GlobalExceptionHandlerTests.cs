using Xunit;
using Microsoft.AspNetCore.Http;
using UserManagementAPI.Middleware;
using Moq;
using System.Text;

namespace UserManagementAPI.Tests
{
    public class GlobalExceptionHandlerTests
    {
        private readonly Mock<ILogger<GlobalExceptionHandler>> _mockLogger;

        public GlobalExceptionHandlerTests()
        {
            _mockLogger = new Mock<ILogger<GlobalExceptionHandler>>();
        }

        [Fact]
        public async Task InvokeAsync_WhenNoExceptionThrown_CallsNextMiddleware()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var nextCalled = false;
            RequestDelegate next = async (HttpContext ctx) =>
            {
                nextCalled = true;
                ctx.Response.StatusCode = 200;
                await ctx.Response.WriteAsync("Success");
            };

            var middleware = new GlobalExceptionHandler(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.True(nextCalled);
            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithInvalidOperationException_Returns400BadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new InvalidOperationException("Test invalid operation");
            };

            var middleware = new GlobalExceptionHandler(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(400, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            // Read response body
            context.Response.Body.Position = 0;
            using (var reader = new StreamReader(context.Response.Body))
            {
                var body = await reader.ReadToEndAsync();
                Assert.Contains("Bad request", body);
                Assert.Contains("Test invalid operation", body);
            }
        }

        [Fact]
        public async Task InvokeAsync_WithArgumentNullException_Returns400BadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new ArgumentNullException("userId", "User ID is required");
            };

            var middleware = new GlobalExceptionHandler(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(400, context.Response.StatusCode);
        }

        [Fact]
        public async Task InvokeAsync_WithKeyNotFoundException_Returns404NotFound()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new KeyNotFoundException("User not found");
            };

            var middleware = new GlobalExceptionHandler(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(404, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            context.Response.Body.Position = 0;
            using (var reader = new StreamReader(context.Response.Body))
            {
                var body = await reader.ReadToEndAsync();
                Assert.Contains("Resource not found", body);
            }
        }

        [Fact]
        public async Task InvokeAsync_WithUnhandledException_Returns500InternalServerError()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new Exception("Unexpected error");
            };

            var middleware = new GlobalExceptionHandler(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal(500, context.Response.StatusCode);
            Assert.Equal("application/json", context.Response.ContentType);

            context.Response.Body.Position = 0;
            using (var reader = new StreamReader(context.Response.Body))
            {
                var body = await reader.ReadToEndAsync();
                Assert.Contains("An internal server error occurred", body);
                // Should not expose stack trace details in production
                Assert.DoesNotContain("Unexpected error", body);
            }
        }

        [Fact]
        public async Task InvokeAsync_WithException_LogsErrorMessage()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var exceptionMessage = "Test exception message";
            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new Exception(exceptionMessage);
            };

            var middleware = new GlobalExceptionHandler(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An unhandled exception occurred")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task InvokeAsync_ResponseContentTypeIsJson()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new InvalidOperationException("Test");
            };

            var middleware = new GlobalExceptionHandler(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.Equal("application/json", context.Response.ContentType);
        }

        [Fact]
        public async Task InvokeAsync_ExceptionResponse_IncludesErrorProperty()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new InvalidOperationException("Operation failed");
            };

            var middleware = new GlobalExceptionHandler(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.Body.Position = 0;
            using (var reader = new StreamReader(context.Response.Body))
            {
                var body = await reader.ReadToEndAsync();
                Assert.Contains("\"error\"", body);
                Assert.Contains("\"details\"", body);
            }
        }

        [Fact]
        public async Task InvokeAsync_UnhandledExceptionDoesNotExposeSensitiveDetails()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var sensitiveInfo = "Database connection string here";
            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new Exception(sensitiveInfo);
            };

            var middleware = new GlobalExceptionHandler(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.Body.Position = 0;
            using (var reader = new StreamReader(context.Response.Body))
            {
                var body = await reader.ReadToEndAsync();
                Assert.DoesNotContain(sensitiveInfo, body);
                Assert.Contains("Please try again later", body);
            }
        }

        [Fact]
        public async Task InvokeAsync_WithInvalidOperationException_ExposesMessage()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var exceptionMessage = "Email already exists";
            RequestDelegate next = (HttpContext ctx) =>
            {
                throw new InvalidOperationException(exceptionMessage);
            };

            var middleware = new GlobalExceptionHandler(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.Body.Position = 0;
            using (var reader = new StreamReader(context.Response.Body))
            {
                var body = await reader.ReadToEndAsync();
                Assert.Contains(exceptionMessage, body);
            }
        }
    }
}
