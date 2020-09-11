using Castle.DynamicProxy;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace StrongInject.Tests.Integration
{
    public partial class DynamicProxyDecoratorTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public DynamicProxyDecoratorTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public class Interceptor : IInterceptor
        {
            private readonly List<string> _output;

            public Interceptor(List<string> output)
            {
                _output = output;
            }

            public void Intercept(IInvocation invocation)
            {
                var stopwatch = Stopwatch.StartNew();
                invocation.Proceed();
                stopwatch.Stop();
                _output.Add($"Call to {invocation.TargetType}.{invocation.Method.Name} took {stopwatch.ElapsedMilliseconds} ms");
            }
        }

        public class Foo { }
        public class Bar { }

        public interface IService1
        {
            void Frob();
            Foo GetFoo();
        }

        public interface IService2
        {
            Bar UseBar(Bar bar);
        }

        public class Service1 : IService1
        {
            public void Frob()
            {
                Thread.Sleep(10);
            }

            public Foo GetFoo()
            {
                Thread.Sleep(20);
                return new Foo();
            }
        }

        public class Service2 : IService2
        {
            public Bar UseBar(Bar bar)
            {
                Thread.Sleep(30);
                return bar;
            }
        }

        [Register(typeof(Service1), typeof(IService1))]
        [Register(typeof(Service2), typeof(IService2))]
        public partial class Container : IContainer<IService1>, IContainer<IService2>
        {
            private readonly IInterceptor _interceptor;
            private readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();

            public Container(IInterceptor interceptor)
            {
                _interceptor = interceptor;
            }

            [DecoratorFactory]
            T Time<T>(T t) where T : class
            {
                if (typeof(T).IsInterface)
                {
                    return _proxyGenerator.CreateInterfaceProxyWithTarget(t, _interceptor);
                }
                else
                {
                    return _proxyGenerator.CreateClassProxyWithTarget(t, _interceptor);
                }
            }
        }

        [Fact]
        public void Test()
        {
            var output = new List<string>();
            var container = new Container(new Interceptor(output));
            container.Run<IService1>(x =>
            {
                x.Frob();
                _ = x.GetFoo();
            });
            container.Run<IService2>(x => _ = x.UseBar(new Bar()));
            Assert.Equal(3, output.Count);
            foreach (var line in output)
                _testOutputHelper.WriteLine(line);
        }
    }
}
