using UserManagementAPI.Interfaces;
using UserManagementAPI.Models;

namespace UserManagementAPI.Services
{
    public class UserService : IUserService
    {
        private static readonly object _lock = new object();
        private static Dictionary<int, User> _usersById = new Dictionary<int, User>();
        private static Dictionary<string, User> _usersByEmail = new Dictionary<string, User>();
        private static int _nextId = 1;

        public Task<User> GetUserByIdAsync(int id)
        {
            lock (_lock)
            {
                _usersById.TryGetValue(id, out var user);
                return Task.FromResult(user);
            }
        }

        public Task<List<User>> GetAllUsersAsync()
        {
            lock (_lock)
            {
                return Task.FromResult(new List<User>(_usersById.Values));
            }
        }

        public Task<PaginatedResponse<User>> GetAllUsersAsync(int pageNumber, int pageSize)
        {
            if (pageNumber <= 0) pageNumber = 1;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Cap at 100 to prevent excessive data retrieval

            lock (_lock)
            {
                var totalCount = _usersById.Count;
                var users = _usersById.Values
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var response = new PaginatedResponse<User>(users, totalCount, pageNumber, pageSize);
                return Task.FromResult(response);
            }
        }

        public Task<User> CreateUserAsync(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "User object cannot be null");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new InvalidOperationException("Email is required");
            }

            lock (_lock)
            {
                string emailKey = user.Email.ToLower();
                if (_usersByEmail.ContainsKey(emailKey))
                {
                    throw new InvalidOperationException("Email already exists");
                }

                user.Id = _nextId++;
                _usersById[user.Id] = user;
                _usersByEmail[emailKey] = user;
                return Task.FromResult(user);
            }
        }

        public Task<User> UpdateUserAsync(int id, User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user), "User object cannot be null");
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                throw new InvalidOperationException("Email is required");
            }

            lock (_lock)
            {
                if (!_usersById.TryGetValue(id, out var existingUser))
                {
                    return Task.FromResult<User>(null);
                }

                string newEmailKey = user.Email.ToLower();
                string oldEmailKey = existingUser.Email.ToLower();

                // Check if email is taken by another user
                if (!oldEmailKey.Equals(newEmailKey) && _usersByEmail.ContainsKey(newEmailKey))
                {
                    throw new InvalidOperationException("Email already exists");
                }

                // Update dictionaries
                if (!oldEmailKey.Equals(newEmailKey))
                {
                    _usersByEmail.Remove(oldEmailKey);
                    _usersByEmail[newEmailKey] = existingUser;
                }

                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.Email = user.Email;
                existingUser.Department = user.Department;

                return Task.FromResult(existingUser);
            }
        }

        public Task<bool> DeleteUserAsync(int id)
        {
            lock (_lock)
            {
                if (!_usersById.TryGetValue(id, out var user))
                {
                    return Task.FromResult(false);
                }

                _usersById.Remove(id);
                _usersByEmail.Remove(user.Email.ToLower());
                return Task.FromResult(true);
            }
        }
    }
}
