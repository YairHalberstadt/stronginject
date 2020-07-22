using StrongInject;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public partial class TestInitializeAsyncReadmeExample
{
    public interface IDb
    {
        Task<Dictionary<string, string>> GetUserPasswordsAsync();
    }

    public class PasswordChecker : IRequiresInitialization, IAsyncDisposable
    {
        private readonly IDb _db;

        private Dictionary<string, string> _userPasswords = default!;

        private Timer _timer = default!;

        public PasswordChecker(IDb db)
        {
            _db = db;
        }

        public async ValueTask InitializeAsync()
        {
            _userPasswords = await _db.GetUserPasswordsAsync();
            _timer = new Timer(async _ => { _userPasswords = await _db.GetUserPasswordsAsync(); }, null, 60000, 60000);
        }

        public bool CheckPassword(string user, string password) => _userPasswords.TryGetValue(user, out var correctPassword) && password == correctPassword;

        public ValueTask DisposeAsync()
        {
            return _timer.DisposeAsync();
        }
    }

    public class DbInstanceProvider : IInstanceProvider<IDb>
    {
        private readonly IDb _db;

        public DbInstanceProvider(IDb db)
        {
            _db = db;
        }

        public ValueTask<IDb> GetAsync()
        {
            return new ValueTask<IDb>(_db);
        }

        public ValueTask ReleaseAsync(IDb instance)
        {
            return default;
        }
    }

    [Registration(typeof(PasswordChecker), Scope.SingleInstance)]
    public partial class Container : IContainer<PasswordChecker>
    {
        public DbInstanceProvider _dbInstanceProvider;

        public Container(DbInstanceProvider dbInstanceProvider)
        {
            _dbInstanceProvider = dbInstanceProvider;
        }
    }

    private class MockDb : IDb
    {
        public Task<Dictionary<string, string>> GetUserPasswordsAsync()
        {
            return Task.FromResult(new Dictionary<string, string> { ["user"] = "password" });
        }
    }

    public async Task Test()
    {
        var container = new Container(new DbInstanceProvider(new MockDb()));
        await container.RunAsync(x =>
        {
            Assert.True(x.CheckPassword("user", "password"));
            Assert.False(x.CheckPassword("user", "p@ssw0rd"));
        });
    }
}
