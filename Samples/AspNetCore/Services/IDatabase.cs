using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrongInject.Samples.AspNetCore.Services
{
    public interface IDatabase
    {
        public Task<IEnumerable<T>> Get<T>();
    }
}
