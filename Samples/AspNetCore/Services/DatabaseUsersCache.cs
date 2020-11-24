using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace StrongInject.Samples.AspNetCore.Services
{
    public class DatabaseUsersCache : IUsersCache, IDisposable
    {
        private Timer? _timer;

        private ValueTask<IEnumerable<User>> _users;

        public DatabaseUsersCache(IDatabase database)
        {
            _users = new ValueTask<IEnumerable<User>>(database.Get<User>());
            _timer = new Timer(async _ => 
            { 
                var users = await database.Get<User>();
                _users = new ValueTask<IEnumerable<User>>(users);
            }, null, 60000, 60000);
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;

        }

        ValueTask<IEnumerable<User>> IUsersCache.GetUsersList()
        {
            return _users;
        }
    }
}
