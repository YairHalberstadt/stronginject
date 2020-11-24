using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrongInject.Samples.AspNetCore.Services
{
    public interface IUsersCache
    {
        public ValueTask<IEnumerable<User>> GetUsersList();
    }
}
