using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

namespace StrongInject.Tests.Integration
{
    /// <summary>
    /// These tests confirm the examples in README.md compile, and when appropriate run correctly
    /// </summary>
    public partial class TestReadmeExamples_AsOfCommit_008a07c7b075e72d491a9144e2f4233aff5a7b7b
    {
        public partial class DeclaringAndUsingAContainer1
        {
            public class A { }

            [Register(typeof(A))]
            public partial class Container : IAsyncContainer<A> { }

            [Fact]
            public static async Task Test()
            {
                System.Console.WriteLine(await new Container().RunAsync(x => x.ToString()));
            }
        }

        public partial class DeclaringAContainer2
        {
            public class A { }
            public class B { }

            [Register(typeof(A))]
            [Register(typeof(B))]
            public partial class Container : IAsyncContainer<A>, IAsyncContainer<B> { }
        }

        public partial class Registration2
        {
            public class BaseBase { }
            public interface IBase { }
            public class Base : BaseBase, IBase { }
            public interface IA { }
            public class A : Base, IA { }

            [Register(typeof(A), typeof(IA), typeof(IBase), typeof(BaseBase))]
            public partial class Container : IAsyncContainer<BaseBase> { }
        }

        public partial class Scope1
        {
            public class A { }
            public interface IB { }
            public class B : IB { }

            [Register(typeof(A), Scope.SingleInstance)]
            [Register(typeof(B), Scope.InstancePerResolution, typeof(IB))]
            public partial class Container : IAsyncContainer<A>, IAsyncContainer<IB> { }
        }

        public partial class Modules1
        {
            public class A { }

            [Register(typeof(A))]
            public class Module { }

            [RegisterModule(typeof(Module))]
            public partial class Container : IAsyncContainer<A> { }
        }

        public partial class FactoryRegistrations1
        {
            public interface IInterface { }
            public class A : IInterface { }
            public class B : IInterface { }
            public record InterfaceArrayFactory(A A, B B) : IAsyncFactory<IInterface[]>
            {
                public ValueTask<IInterface[]> CreateAsync() => new ValueTask<IInterface[]>(new IInterface[] { A, B });

                public ValueTask ReleaseAsync(IInterface[] instance)
                {
                    return Helpers.DisposeAsync(instance);
                }
            }

            [Register(typeof(A))]
            [Register(typeof(B))]
            [RegisterFactory(typeof(InterfaceArrayFactory))]
            public partial class Container : IAsyncContainer<IInterface[]> { }

            [Fact]
            public async Task Test()
            {
                var container = new Container();
                await container.RunAsync(x =>
                {
                    Assert.Equal(2, x.Length);
                    Assert.IsType<A>(x[0]);
                    Assert.IsType<B>(x[1]);
                });
            }
        }

        public partial class FactoryRegistrations2
        {
            public interface IInterface { }
            public class A : IInterface { }
            public class B : IInterface { }
            public record InterfaceArrayFactory(A A, B B) : IAsyncFactory<IInterface[]>
            {
                public ValueTask<IInterface[]> CreateAsync() => new ValueTask<IInterface[]>(new IInterface[] { A, B });

                public ValueTask ReleaseAsync(IInterface[] instance)
                {
                    return Helpers.DisposeAsync(instance);
                }
            }

            [Register(typeof(A))]
            [Register(typeof(B))]
            [RegisterFactory(typeof(InterfaceArrayFactory), factoryScope: Scope.SingleInstance, factoryTargetScope: Scope.InstancePerResolution)]
            public partial class Container : IAsyncContainer<IInterface[]> { }

            [Fact]
            public async Task Test()
            {
                var container = new Container();
                var first = await container.RunAsync(x =>
                {
                    Assert.Equal(2, x.Length);
                    Assert.IsType<A>(x[0]);
                    Assert.IsType<B>(x[1]);
                    return x;
                });
                var second = await container.RunAsync(x =>
                {
                    Assert.Equal(2, x.Length);
                    Assert.IsType<A>(x[0]);
                    Assert.IsType<B>(x[1]);
                    return x;
                });
                Assert.NotSame(first, second);
            }
        }

        public partial class RuntimeConfiguration1
        {
            public interface IInterface { }
            public class A : IInterface { }
            public class B : IInterface { }

            public enum InterfaceToUse
            {
                UseA,
                UseB
            }

            public record InstanceFactory(InterfaceToUse InterfaceToUse) : IAsyncFactory<InterfaceToUse>
            {
                public ValueTask<InterfaceToUse> CreateAsync() => new ValueTask<InterfaceToUse>(InterfaceToUse);

                public ValueTask ReleaseAsync(InterfaceToUse instance) => default;
            }

            public record InterfaceFactory(A A, B B, InterfaceToUse InterfaceToUse) : IAsyncFactory<IInterface>
            {
                public ValueTask<IInterface> CreateAsync() => new ValueTask<IInterface>(InterfaceToUse == InterfaceToUse.UseA ? (IInterface)A : B);

                public ValueTask ReleaseAsync(IInterface instance)
                {
                    return Helpers.DisposeAsync(instance);
                }
            }

            [Register(typeof(A))]
            [Register(typeof(B))]
            [RegisterFactory(typeof(InterfaceFactory))]
            public partial class Container : IAsyncContainer<IInterface>
            {
                [Instance(Options.AsImplementedInterfacesAndUseAsFactory)] private readonly InstanceFactory _instanceProvider;
                public Container(InstanceFactory instanceProvider) => _instanceProvider = instanceProvider;
            }

            [Fact]
            public async Task Test()
            {
                await new Container(new InstanceFactory(InterfaceToUse.UseA)).RunAsync(x =>
                {
                    Assert.IsType<A>(x);
                });

                await new Container(new InstanceFactory(InterfaceToUse.UseB)).RunAsync(x =>
                {
                    Assert.IsType<B>(x);
                });
            }
        }

        public partial class InitializeAsync1
        {
            public interface IDb
            {
                Task<Dictionary<string, string>> GetUserPasswordsAsync();
            }

            public class PasswordChecker : IRequiresAsyncInitialization, IAsyncDisposable
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

            public class DbFactory : IAsyncFactory<IDb>
            {
                private readonly IDb _db;

                public DbFactory(IDb db)
                {
                    _db = db;
                }

                public ValueTask<IDb> CreateAsync()
                {
                    return new ValueTask<IDb>(_db);
                }

                public ValueTask ReleaseAsync(IDb instance)
                {
                    return default;
                }
            }

            [Register(typeof(PasswordChecker), Scope.SingleInstance)]
            public partial class Container : IAsyncContainer<PasswordChecker>
            {
                [Instance(Options.AsImplementedInterfacesAndUseAsFactory)] public DbFactory _dbInstanceProvider;

                public Container(DbFactory dbInstanceProvider)
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
                var container = new Container(new DbFactory(new MockDb()));
                await container.RunAsync(x =>
                {
                    Assert.True(x.CheckPassword("user", "password"));
                    Assert.False(x.CheckPassword("user", "p@ssw0rd"));
                });
            }
        }
    }
}
