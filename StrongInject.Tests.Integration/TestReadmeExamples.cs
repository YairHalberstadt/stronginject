using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Xunit;

#nullable disable
namespace StrongInject.Tests.Integration
{
    /// <summary>
    /// These tests confirm the examples in README.md compile, and when appropriate run correctly
    /// </summary>
    public partial class TestReadmeExamples
    {
        public partial class DeclaringAndUsingAContainer1
        {
            public class A { }

            [Register(typeof(A))]
            public partial class Container : IContainer<A> { }

            [Fact]
            public static void Test()
            {
                Console.WriteLine(new Container().Run(x => x.ToString()));
            }
        }

        public partial class DeclaringAContainer2
        {
            public class A { }
            public class B { }

            [Register(typeof(A))]
            [Register(typeof(B))]
            public partial class Container : IContainer<A>, IContainer<B> { }
        }

        public partial class Registration2
        {
            public class BaseBase { }
            public interface IBase { }
            public class Base : BaseBase, IBase { }
            public interface IA { }
            public class A : Base, IA { }

            [Register(typeof(A), typeof(IA), typeof(IBase), typeof(BaseBase))]
            public partial class Container : IContainer<BaseBase> { }
        }

        public partial class Scope1
        {
            public class A { }
            public interface IB { }
            public class B : IB { }

            [Register(typeof(A), Scope.SingleInstance)]
            [Register(typeof(B), Scope.InstancePerResolution, typeof(IB))]
            public partial class Container : IContainer<A>, IContainer<IB> { }
        }

        public partial class Modules1
        {
            public class A { }

            [Register(typeof(A))]
            public class Module { }

            [RegisterModule(typeof(Module))]
            public partial class Container : IContainer<A> { }
        }

        public partial class FactoryRegistrations1
        {
            public interface IInterface { }
            public class A : IInterface { }
            public class B : IInterface { }
            public record InterfaceArrayFactory(A A, B B) : IFactory<IInterface[]>
            {
                public IInterface[] Create() => new IInterface[] { A, B };

                public void Release(IInterface[] instance)
                {
                    Helpers.Dispose(instance);
                }
            }

            [Register(typeof(A))]
            [Register(typeof(B))]
            [RegisterFactory(typeof(InterfaceArrayFactory))]
            public partial class Container : IContainer<IInterface[]> { }

            [Fact]
            public void Test()
            {
                var container = new Container();
                container.Run(x =>
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
            public record InterfaceArrayFactory(A A, B B) : IFactory<IInterface[]>
            {
                public IInterface[] Create() => new IInterface[] { A, B };

                public void Release(IInterface[] instance)
                {
                    Helpers.Dispose(instance);
                }
            }

            [Register(typeof(A))]
            [Register(typeof(B))]
            [RegisterFactory(typeof(InterfaceArrayFactory), factoryScope: Scope.SingleInstance, factoryTargetScope: Scope.InstancePerResolution)]
            public partial class Container : IContainer<IInterface[]> { }

            [Fact]
            public void Test()
            {
                var container = new Container();
                var first = container.Run(x =>
                {
                    Assert.Equal(2, x.Length);
                    Assert.IsType<A>(x[0]);
                    Assert.IsType<B>(x[1]);
                    return x;
                });
                var second = container.Run(x =>
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

            public record InstanceFactory(InterfaceToUse InterfaceToUse) : IFactory<InterfaceToUse>
            {
                public InterfaceToUse Create() => InterfaceToUse;

                public void Release(InterfaceToUse instance) {}
            }

            public record InterfaceFactory(A A, B B, InterfaceToUse InterfaceToUse) : IFactory<IInterface>
            {
                public IInterface Create() => InterfaceToUse == InterfaceToUse.UseA ? (IInterface)A : B;

                public void Release(IInterface instance)
                {
                    Helpers.Dispose(instance);
                }
            }

            [Register(typeof(A))]
            [Register(typeof(B))]
            [RegisterFactory(typeof(InterfaceFactory))]
            public partial class Container : IContainer<IInterface>
            {
                [Instance(Options.AsImplementedInterfacesAndUseAsFactory)] private readonly InstanceFactory _instanceProvider;
                public Container(InstanceFactory instanceProvider) => _instanceProvider = instanceProvider;
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

                private Dictionary<string, string> _userPasswords;

                private Timer _timer;

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

            public record DbFactory(IDb Db) : IFactory<IDb>
            {
                public IDb Create()
                {
                    return Db;
                }

                public void Release(IDb instance) { }
            }

            [Register(typeof(PasswordChecker), Scope.SingleInstance)]
            public partial class Container : IAsyncContainer<PasswordChecker>
            {
                [Instance(Options.AsImplementedInterfacesAndUseAsFactory)] private readonly DbFactory _dbInstanceProvider;

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
