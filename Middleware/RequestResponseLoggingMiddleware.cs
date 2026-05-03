using System.Diagnostics;
using System.Text;

namespace UserManagementAPI.Middleware
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = context.TraceIdentifier;

            try
            {
                // Log incoming request
                await LogRequestAsync(context, requestId);

                // Copy the original response stream so we can read it
                var originalBodyStream = context.Response.Body;

                using (var responseBody = new MemoryStream())
                {
                    context.Response.Body = responseBody;

                    // Call the next middleware
                    await _next(context);

                    stopwatch.Stop();

                    // Log response
                    await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds);

                    // Copy the response back to the original stream
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError($"[{requestId}] Exception occurred: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private async Task LogRequestAsync(HttpContext context, string requestId)
        {
            context.Request.EnableBuffering();

            var request = context.Request;
            var sb = new StringBuilder();
            sb.AppendLine($"[{requestId}] ===== HTTP REQUEST =====");
            sb.AppendLine($"Method: {request.Method}");
            sb.AppendLine($"Path: {request.Path}");
            sb.AppendLine($"QueryString: {request.QueryString}");
            sb.AppendLine($"Scheme: {request.Scheme}");
            sb.AppendLine($"Host: {request.Host}");

            // Log headers (exclude sensitive ones)
            sb.AppendLine("Headers:");
            foreach (var header in request.Headers)
            {
                if (!IsSensitiveHeader(header.Key))
                {
                    sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
            }

            // Log request body if it's JSON or form data
            if (request.ContentLength > 0 && IsLoggableContentType(request.ContentType))
            {
                request.Body.Position = 0;
                using (var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true))
                {
                    var body = await reader.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        sb.AppendLine($"Body: {body}");
                    }
                    request.Body.Position = 0;
                }
            }

            sb.AppendLine("==========================");
            _logger.LogInformation(sb.ToString());
        }

        private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMilliseconds)
        {
            var response = context.Response;
            var sb = new StringBuilder();
            sb.AppendLine($"[{requestId}] ===== HTTP RESPONSE =====");
            sb.AppendLine($"StatusCode: {response.StatusCode}");
            sb.AppendLine($"Elapsed Time: {elapsedMilliseconds}ms");

            // Log headers
            sb.AppendLine("Headers:");
            foreach (var header in response.Headers)
            {
                if (!IsSensitiveHeader(header.Key))
                {
                    sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
                }
            }

            // Log response body if it's JSON or form data
            if (response.ContentLength > 0 && IsLoggableContentType(response.ContentType))
            {
                response.Body.Position = 0;
                using (var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true))
                {
                    var body = await reader.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        sb.AppendLine($"Body: {body}");
                    }
                    response.Body.Position = 0;
                }
            }

            sb.AppendLine("==========================");

            if (response.StatusCode >= 400)
            {
                _logger.LogWarning(sb.ToString());
            }
            else
            {
                _logger.LogInformation(sb.ToString());
            }
        }

        private static bool IsSensitiveHeader(string headerName)
        {
            var sensitiveHeaders = new[] { "Authorization", "Cookie", "X-API-Key", "Password" };
            return sensitiveHeaders.Any(h => h.Equals(headerName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsLoggableContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return false;

            var loggableTypes = new[] { "application/json", "application/x-www-form-urlencoded", "text/plain" };
            return loggableTypes.Any(t => contentType.Contains(t, StringComparison.OrdinalIgnoreCase));
        }
    }
}
