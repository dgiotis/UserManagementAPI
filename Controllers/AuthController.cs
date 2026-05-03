using Microsoft.AspNetCore.Mvc;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        /// <summary>
        /// Login and get a token (for testing purposes)
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Simple mock login for testing
            if (string.IsNullOrEmpty(request?.Username) || string.IsNullOrEmpty(request?.Password))
            {
                return BadRequest(new { error = "Username and password are required" });
            }

            // Mock credentials (replace with real authentication in production)
            if (request.Username == "admin" && request.Password == "password123")
            {
                var token = "test-token-123";
                return Ok(new
                {
                    token = token,
                    message = "Login successful. Use this token in the Authorization header: Bearer " + token
                });
            }

            if (request.Username == "user" && request.Password == "user123")
            {
                var token = "demo-token-456";
                return Ok(new
                {
                    token = token,
                    message = "Login successful. Use this token in the Authorization header: Bearer " + token
                });
            }

            return Unauthorized(new { error = "Invalid credentials" });
        }

        /// <summary>
        /// Get current user info (requires valid token)
        /// </summary>
        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var token = HttpContext.Items["Token"]?.ToString();

            return Ok(new
            {
                token = token,
                message = "You are authenticated",
                user = new { username = "authenticated_user", role = "admin" }
            });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
