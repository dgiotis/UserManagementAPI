using Xunit;
using Microsoft.AspNetCore.Mvc;
using UserManagementAPI.Models;
using UserManagementAPI.Controllers;
using UserManagementAPI.Services;
using UserManagementAPI.Interfaces;

namespace UserManagementAPI.Tests
{
    public class UsersControllerTests
    {
        private readonly UsersController _controller;
        private readonly IUserService _userService;

        public UsersControllerTests()
        {
            _userService = new UserService();
            _controller = new UsersController(_userService);
        }

        #region POST (Create) Tests

        [Fact]
        public async Task CreateUser_WithValidUser_ReturnsCreatedAtAction()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Department = "Engineering"
            };

            // Act
            var result = await _controller.CreateUser(user);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(UsersController.GetUserById), createdResult.ActionName);
            Assert.NotNull(createdResult.Value);
        }

        [Fact]
        public async Task CreateUser_WithNullUser_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.CreateUser(null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("User object is required", badRequestResult.Value);
        }

        [Fact]
        public async Task CreateUser_WithDuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var user1 = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Department = "Engineering"
            };

            var user2 = new User
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "john@example.com",
                Department = "HR"
            };

            await _controller.CreateUser(user1);

            // Act
            var result = await _controller.CreateUser(user2);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        #endregion

        #region GET Tests

        [Fact]
        public async Task GetUserById_WithValidId_ReturnsOkWithUser()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Department = "Engineering"
            };

            var createdResult = await _controller.CreateUser(user);
            var createdUser = ((CreatedAtActionResult)createdResult.Result).Value as User;

            // Act
            var result = await _controller.GetUserById(createdUser.Id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = okResult.Value as User;
            Assert.NotNull(returnedUser);
            Assert.Equal("John", returnedUser.FirstName);
        }

        [Fact]
        public async Task GetUserById_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetUserById(999);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetUserById_WithNegativeId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.GetUserById(-1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetAllUsers_ReturnsOkWithPaginatedResponse()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Department = "Engineering"
            };

            await _controller.CreateUser(user);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = okResult.Value as PaginatedResponse<User>;
            Assert.NotNull(response);
            Assert.True(response.Data.Count > 0);
        }

        #endregion

        #region PUT (Update) Tests

        [Fact]
        public async Task UpdateUser_WithValidUser_ReturnsOkWithUpdatedUser()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Department = "Engineering"
            };

            var createdResult = await _controller.CreateUser(user);
            var createdUser = ((CreatedAtActionResult)createdResult.Result).Value as User;

            var updatedUser = new User
            {
                FirstName = "Jonathan",
                LastName = "Doe",
                Email = "jonathan@example.com",
                Department = "Management"
            };

            // Act
            var result = await _controller.UpdateUser(createdUser.Id, updatedUser);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = okResult.Value as User;
            Assert.NotNull(returnedUser);
            Assert.Equal("Jonathan", returnedUser.FirstName);
        }

        [Fact]
        public async Task UpdateUser_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var updatedUser = new User
            {
                FirstName = "Jonathan",
                LastName = "Doe",
                Email = "jonathan@example.com",
                Department = "Management"
            };

            // Act
            var result = await _controller.UpdateUser(999, updatedUser);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UpdateUser_WithNullUser_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.UpdateUser(1, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateUser_WithNegativeId_ReturnsBadRequest()
        {
            // Arrange
            var updatedUser = new User
            {
                FirstName = "Jonathan",
                LastName = "Doe",
                Email = "jonathan@example.com",
                Department = "Management"
            };

            // Act
            var result = await _controller.UpdateUser(-1, updatedUser);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        #endregion

        #region DELETE Tests

        [Fact]
        public async Task DeleteUser_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Department = "Engineering"
            };

            var createdResult = await _controller.CreateUser(user);
            var createdUser = ((CreatedAtActionResult)createdResult.Result).Value as User;

            // Act
            var result = await _controller.DeleteUser(createdUser.Id);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteUser_WithInvalidId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.DeleteUser(999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteUser_WithNegativeId_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.DeleteUser(-1);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion
    }
}
