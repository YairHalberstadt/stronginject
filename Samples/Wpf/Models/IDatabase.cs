using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrongInject.Samples.Wpf.Models
{
    public interface IDatabase
    {
        Task<IEnumerable<User>> GetUsers();
        Task<bool> DeleteUser(Guid userId);
        Task AddOrUpdateUser(User user);
    }
}
