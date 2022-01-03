using System;
using System.Threading.Tasks;
using Xunit;

namespace StrongInject.Tests.Integration
{
    public partial class TestContainerExtensions
    {
        public class A : IDisposable
        {
            public bool IsDisposed { get; private set; }
            public string M1() => "M1";

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        [Register<A>]
        public partial class Container : IContainer<A>
        {
        }

        [Fact]
        public void TestRunFunc()
        {
            A? a = null;
            Assert.Equal("M1", new Container().Run(x =>
            {
                a = x;
                Assert.False(a.IsDisposed);
                return x.M1();
            }));
            Assert.True(a!.IsDisposed);
        }
        
        [Fact]
        public void TestRunAction()
        {
            A? a = null;
            new Container().Run(x =>
            {
                a = x;
                Assert.False(a.IsDisposed);
                Assert.Equal("M1", x.M1());
            });
            
            Assert.True(a!.IsDisposed);
        }
        
        [Fact]
        public void TestRunFunc_T_TParam_TResult()
        {
            A? a = null;
            Assert.Equal("Param M1", new Container().Run((x, prm) =>
            {
                a = x;
                Assert.False(a.IsDisposed);
                return prm + " " + x.M1();
            }, "Param"));
            
            Assert.True(a!.IsDisposed);
        }
        
        [Fact]
        public void TestResolve()
        {
            A? a = null;
            using (var ownedOfA = new Container().Resolve())
            {
                a = ownedOfA.Value;
                Assert.False(a.IsDisposed);
                Assert.Equal("M1", a.M1());
            }
            Assert.True(a.IsDisposed);
        }
    }
    
    public partial class TestAsyncContainerExtensions
    {
        public class A : IDisposable, IRequiresAsyncInitialization, IAsyncDisposable
        {
            public bool IsDisposed { get; private set; }
            public string M1() => "M1";

            public void Dispose()
            {
                throw new Exception("Should call DisposeAsync not Dispose");
            }

            public async ValueTask InitializeAsync()
            {
                await Task.Yield();
            }

            public async ValueTask DisposeAsync()
            {
                await Task.Yield();
                IsDisposed = true;
            }
        }

        [Register<A>]
        public partial class Container : IAsyncContainer<A>
        {
            
        }

        [Fact]
        public async Task TestRunAsyncFunc()
        {
            A? a = null;
            Assert.Equal("M1", await new Container().RunAsync(x =>
            {
                a = x;
                Assert.False(a.IsDisposed);
                return x.M1();
            }));
            Assert.True(a!.IsDisposed);
        }
        
        [Fact]
        public async Task TestRunAsyncAction()
        {
            A? a = null;
            await new Container().RunAsync(x =>
            {
                a = x;
                Assert.False(a.IsDisposed);
                Assert.Equal("M1", x.M1());
            });
            
            Assert.True(a!.IsDisposed);
        }
        
        [Fact]
        public async Task TestRunFunc_T_TParam_ValueTaskOfTResult()
        {
            A? a = null;
            Assert.Equal("Param M1", await new Container().RunAsync(async (x, prm) =>
            {
                await Task.Yield();
                a = x;
                Assert.False(a.IsDisposed);
                return prm + " " + x.M1();
            }, "Param"));
            
            Assert.True(a!.IsDisposed);
        }
        
        [Fact]
        public async Task TestRunAsyncFunc_ValueTaskOfTResult()
        {
            A? a = null;
            Assert.Equal("M1", await new Container().RunAsync(async Task<string> (A x) =>
            {
                await Task.Yield();
                a = x;
                Assert.False(a.IsDisposed);
                return x.M1();
            }));
            Assert.True(a!.IsDisposed);
        }
        
        [Fact]
        public async Task TestRunAsyncFunc_TaskOfTResult()
        {
            A? a = null;
            Assert.Equal("M1", await new Container().RunAsync(async ValueTask<string> (A x) =>
            {
                await Task.Yield();
                a = x;
                Assert.False(a.IsDisposed);
                return x.M1();
            }));
            Assert.True(a!.IsDisposed);
        }
        
        [Fact]
        public async Task TestRunAsyncFunc_ValueTask()
        {
            A? a = null;
            await new Container().RunAsync(async ValueTask (A x) =>
            {
                await Task.Yield();
                a = x;
                Assert.False(a.IsDisposed);
                Assert.Equal("M1", x.M1());
            });
            
            Assert.True(a!.IsDisposed);
        }
        
        [Fact]
        public async Task TestRunAsyncFunc_Task()
        {
            A? a = null;
            await new Container().RunAsync(async Task (A x) =>
            {
                await Task.Yield();
                a = x;
                Assert.False(a.IsDisposed);
                Assert.Equal("M1", x.M1());
            });
            
            Assert.True(a!.IsDisposed);
        }
        
        [Fact]
        public async Task TestResolveAsync()
        {
            A? a = null;
            await using (var ownedOfA = await new Container().ResolveAsync())
            {
                a = ownedOfA.Value;
                Assert.False(a.IsDisposed);
                Assert.Equal("M1", a.M1());
            }
            Assert.True(a.IsDisposed);
        }
    }
}
