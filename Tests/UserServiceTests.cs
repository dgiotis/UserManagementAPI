using Xunit;
using UserManagementAPI.Models;
using UserManagementAPI.Services;
using UserManagementAPI.Interfaces;

namespace UserManagementAPI.Tests
{
    public class UserServiceTests
    {
        private readonly IUserService _userService;

        public UserServiceTests()
        {
            _userService = new UserService();
        }

        #region Create Tests
        
        [Fact]
        public async Task CreateUserAsync_WithValidUser_ReturnsUserWithId()
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
            var result = await _userService.CreateUserAsync(user);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("John", result.FirstName);
            Assert.Equal("john@example.com", result.Email);
        }

        [Fact]
        public async Task CreateUserAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
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
                Email = "john@example.com", // Same email
                Department = "HR"
            };

            await _userService.CreateUserAsync(user1);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.CreateUserAsync(user2));
            Assert.Equal("Email already exists", ex.Message);
        }

        [Fact]
        public async Task CreateUserAsync_WithNullUser_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _userService.CreateUserAsync(null));
        }

        [Fact]
        public async Task CreateUserAsync_WithNullEmail_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = null,
                Department = "Engineering"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.CreateUserAsync(user));
            Assert.Equal("Email is required", ex.Message);
        }

        [Fact]
        public async Task CreateUserAsync_WithEmptyEmail_ThrowsInvalidOperationException()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "   ",
                Department = "Engineering"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.CreateUserAsync(user));
        }

        #endregion

        #region Get Tests

        [Fact]
        public async Task GetUserByIdAsync_WithValidId_ReturnsUser()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Department = "Engineering"
            };

            var createdUser = await _userService.CreateUserAsync(user);

            // Act
            var result = await _userService.GetUserByIdAsync(createdUser.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("John", result.FirstName);
            Assert.Equal("john@example.com", result.Email);
        }

        [Fact]
        public async Task GetUserByIdAsync_WithInvalidId_ReturnsNull()
        {
            // Act
            var result = await _userService.GetUserByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllUsersAsync_ReturnsAllUsers()
        {
            // Arrange
            var user1 = new User { FirstName = "John", LastName = "Doe", Email = "john@example.com", Department = "Engineering" };
            var user2 = new User { FirstName = "Jane", LastName = "Smith", Email = "jane@example.com", Department = "HR" };

            await _userService.CreateUserAsync(user1);
            await _userService.CreateUserAsync(user2);

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.NotEmpty(result);
            Assert.True(result.Count >= 2);
        }

        [Fact]
        public async Task GetAllUsersAsync_WithPagination_ReturnsPaginatedResult()
        {
            // Arrange
            for (int i = 0; i < 15; i++)
            {
                var user = new User
                {
                    FirstName = $"User{i}",
                    LastName = "Test",
                    Email = $"user{i}@example.com",
                    Department = "Engineering"
                };
                await _userService.CreateUserAsync(user);
            }

            // Act
            var result = await _userService.GetAllUsersAsync(1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.Data.Count);
            Assert.True(result.TotalCount >= 15);
            Assert.True(result.HasNextPage);
            Assert.False(result.HasPreviousPage);
        }

        #endregion

        #region Update Tests

        [Fact]
        public async Task UpdateUserAsync_WithValidUser_UpdatesSuccessfully()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Department = "Engineering"
            };

            var createdUser = await _userService.CreateUserAsync(user);

            var updatedUser = new User
            {
                FirstName = "Jonathan",
                LastName = "Doe",
                Email = "jonathan@example.com",
                Department = "Management"
            };

            // Act
            var result = await _userService.UpdateUserAsync(createdUser.Id, updatedUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Jonathan", result.FirstName);
            Assert.Equal("jonathan@example.com", result.Email);
            Assert.Equal("Management", result.Department);
        }

        [Fact]
        public async Task UpdateUserAsync_WithInvalidId_ReturnsNull()
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
            var result = await _userService.UpdateUserAsync(999, updatedUser);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateUserAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
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
                Email = "jane@example.com",
                Department = "HR"
            };

            var createdUser1 = await _userService.CreateUserAsync(user1);
            var createdUser2 = await _userService.CreateUserAsync(user2);

            var updatedUser = new User
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "john@example.com", // Try to use existing email
                Department = "HR"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _userService.UpdateUserAsync(createdUser2.Id, updatedUser));
            Assert.Equal("Email already exists", ex.Message);
        }

        [Fact]
        public async Task UpdateUserAsync_WithNullUser_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _userService.UpdateUserAsync(1, null));
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task DeleteUserAsync_WithValidId_DeletesSuccessfully()
        {
            // Arrange
            var user = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Department = "Engineering"
            };

            var createdUser = await _userService.CreateUserAsync(user);

            // Act
            var result = await _userService.DeleteUserAsync(createdUser.Id);

            // Assert
            Assert.True(result);

            // Verify deletion
            var deletedUser = await _userService.GetUserByIdAsync(createdUser.Id);
            Assert.Null(deletedUser);
        }

        [Fact]
        public async Task DeleteUserAsync_WithInvalidId_ReturnsFalse()
        {
            // Act
            var result = await _userService.DeleteUserAsync(999);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Email Case Insensitivity Tests

        [Fact]
        public async Task CreateUserAsync_WithDifferentCaseEmail_ThrowsDuplicateException()
        {
            // Arrange
            var user1 = new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "John@Example.com",
                Department = "Engineering"
            };

            var user2 = new User
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "john@example.com", // Different case
                Department = "HR"
            };

            await _userService.CreateUserAsync(user1);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.CreateUserAsync(user2));
        }

        #endregion
    }
}
