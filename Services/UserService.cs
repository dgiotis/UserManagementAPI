using UserManagementAPI.Interfaces;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services
{
    public class UserService : IUserService
    {
        private static List<User> _users = new List<User>();
        private static int _nextId = 1;

        public Task<User> GetUserByIdAsync(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            return Task.FromResult(user);
        }

        public Task<List<User>> GetAllUsersAsync()
        {
            return Task.FromResult(new List<User>(_users));
        }

        public Task<User> CreateUserAsync(User user)
        {
            user.Id = _nextId++;
            _users.Add(user);
            return Task.FromResult(user);
        }

        public Task<User> UpdateUserAsync(int id, User user)
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == id);
            if (existingUser == null)
            {
                return Task.FromResult<User>(null);
            }

            existingUser.FirstName = user.FirstName;
            existingUser.LastName = user.LastName;
            existingUser.Email = user.Email;
            existingUser.Department = user.Department;

            return Task.FromResult(existingUser);
        }

        public Task<bool> DeleteUserAsync(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return Task.FromResult(false);
            }

            _users.Remove(user);
            return Task.FromResult(true);
        }
    }
}
