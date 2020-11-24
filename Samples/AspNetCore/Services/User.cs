using System;

namespace StrongInject.Samples.AspNetCore.Services
{
    public class User
    {
        public User(string name, DateTime dateOfBirth)
        {
            Name = name;
            DateOfBirth = dateOfBirth;
        }

        public string Name { get; }

        public DateTime DateOfBirth { get; }
    }
}
