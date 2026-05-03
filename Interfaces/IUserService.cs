using UserManagementAPI.Models;

namespace UserManagementAPI.Interfaces
{
    public interface IUserService
    {
        Task<User> GetUserByIdAsync(int id);
        Task<List<User>> GetAllUsersAsync();
        Task<User> CreateUserAsync(User user);
        Task<User> UpdateUserAsync(int id, User user);
        Task<bool> DeleteUserAsync(int id);
    }
}
