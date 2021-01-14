using System;

namespace StrongInject.Samples.Wpf.Models
{
    public record User(Guid Id, string FirstName, string LastName)
    {
        public static User CreateNew(string FirstName, string LastName) => new User(Guid.NewGuid(), FirstName, LastName);
    }
}
