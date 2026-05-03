using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Interfaces;
using UserManagementAPI.Models;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Get all users
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// Get a user by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUserById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }

        /// <summary>
        /// Create a new user
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdUser = await _userService.CreateUserAsync(user);
            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
        }

        /// <summary>
        /// Update an existing user
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<User>> UpdateUser(int id, [FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updatedUser = await _userService.UpdateUserAsync(id, user);
            if (updatedUser == null)
            {
                return NotFound();
            }
            return Ok(updatedUser);
        }

        /// <summary>
        /// Delete a user
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            var success = await _userService.DeleteUserAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
