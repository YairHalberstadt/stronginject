using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StrongInject.Samples.AspNetCore.Services
{
    public class MockDatabase : IDatabase
    {
        public async Task<IEnumerable<T>> Get<T>()
        {
            await Task.Yield();

            if (typeof(T) == typeof(User))
            {
                return new[] {
                    new User("Rebekah Riley", DateTime.Parse("1/2/1996")),
                    new User("Gene Simons", DateTime.Parse("3/4/1978")),
                    new User("Nabilah Downs", DateTime.Parse("5/6/2002"))
                }.Cast<T>();
            }

            return Array.Empty<T>();
        }
    }
}
