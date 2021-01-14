using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace StrongInject.Samples.Wpf.Models
{
    public class MockDatabase : IDatabase
    {
        private readonly ConcurrentDictionary<Guid, User> _users = new ConcurrentDictionary<Guid, User>(new []
        {
            User.CreateNew("Erik", "Hawkins"),
            User.CreateNew("Martine", "Guevara"),
            User.CreateNew("Keith", "Lin"),
            User.CreateNew("Jenson", "Fernandez"),
            User.CreateNew("Lynden", "Lancaster"),
            User.CreateNew("Shahid", "Fraser"),
            User.CreateNew("Blair", "Day"),
            User.CreateNew("Sofie", "Workman"),
            User.CreateNew("Abi", "Galloway"),
            User.CreateNew("Kean", "Rutledge"),
        }.Select(x => KeyValuePair.Create(x.Id, x)));

        public async Task AddOrUpdateUser(User user)
        {
            await Task.Yield();
            _users.AddOrUpdate(user.Id, user, (_, _) => user);
        }

        public async Task<bool> DeleteUser(Guid userId)
        {
            await Task.Yield();
            return _users.TryRemove(userId, out _);
        }

        public async Task<IEnumerable<User>> GetUsers()
        {
            await Task.Yield();
            return _users.Values;
        }
    }
}
