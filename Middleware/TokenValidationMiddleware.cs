using System.Net;

namespace UserManagementAPI.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenValidationMiddleware> _logger;

        // Simple in-memory store for valid tokens (in production, use JWT or validate against a service)
        private static readonly HashSet<string> ValidTokens = new()
        {
            "test-token-123",
            "demo-token-456"
        };

        public TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip token validation for certain endpoints
            if (ShouldSkipTokenValidation(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Check for Authorization header
            if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                _logger.LogWarning("Missing Authorization header");
                await RespondWithUnauthorized(context, "Missing Authorization header");
                return;
            }

            // Extract Bearer token
            var token = ExtractBearerToken(authHeader.ToString());
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("Invalid Authorization header format");
                await RespondWithUnauthorized(context, "Invalid Authorization header format");
                return;
            }

            // Validate token
            if (!IsValidToken(token))
            {
                _logger.LogWarning($"Invalid token: {token}");
                await RespondWithUnauthorized(context, "Invalid or expired token");
                return;
            }

            // Token is valid, add to context for later use
            context.Items["Token"] = token;
            _logger.LogInformation("Token validated successfully");

            await _next(context);
        }

        private static bool ShouldSkipTokenValidation(PathString path)
        {
            // Endpoints that don't require token validation
            var exemptPaths = new[]
            {
                "/swagger",
                "/swagger/ui",
                "/swagger/ui/index.html",
                "/swagger/",
                "/api/auth/login",
                "/health",
                "/health/live"
            };

            return exemptPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
        }

        private static string ExtractBearerToken(string authHeader)
        {
            const string bearerScheme = "Bearer ";

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith(bearerScheme, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return authHeader.Substring(bearerScheme.Length).Trim();
        }

        private static bool IsValidToken(string token)
        {
            // Simple validation: check if token exists in valid tokens list
            // In production, validate JWT signature, expiry, etc.
            return ValidTokens.Contains(token);
        }

        private static Task RespondWithUnauthorized(HttpContext context, string message)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

            var response = new
            {
                error = "Unauthorized",
                message = message,
                timestamp = DateTime.UtcNow
            };

            return context.Response.WriteAsJsonAsync(response);
        }
    }
}
