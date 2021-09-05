using System;
using System.Threading.Tasks;
using Xunit;

namespace StrongInject.Tests.Integration
{
    public partial class TestParallelismAndResolutionFailureBehaviour
    {
        public class ActionDisposable<T> : IDisposable
        {
            private readonly Action _onDispose;

            public ActionDisposable(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                _onDispose();
            }
        }

        public partial class TestDisposesOfDependenciesWhenResolutionFailsContainer : IContainer<ActionDisposable<string>>
        {
            public bool IntDisposed { get; private set;  } = false;
            public bool LongDisposed { get; private set; } = false;
            [Factory] ActionDisposable<int> M1() => new ActionDisposable<int>(() => IntDisposed = true);
            [Factory] ActionDisposable<long> M2(ActionDisposable<int> dep) => new ActionDisposable<long>(() => LongDisposed = true);
            [Factory] ActionDisposable<string> M3(ActionDisposable<long> dep) => throw new Exception();
        }

        [Fact]
        public void TestDisposesOfDependenciesWhenResolutionFails()
        {
            var container = new TestDisposesOfDependenciesWhenResolutionFailsContainer();
            Assert.Throws<Exception>(container.Resolve);
            Assert.True(container.IntDisposed);
            Assert.True(container.LongDisposed);
        }

        public partial class TestResolvesInParallelContainer : IAsyncContainer<string>
        {
            public volatile bool _intStarted;
            public volatile bool _longStarted;

            [Factory]
            async Task<int> M1()
            {
                _intStarted = true;
                while (true)
                {
                    await Task.Yield();
                    if (_longStarted)
                        return 42;
                }
            }
            [Factory]
            async Task<long> M2()
            {
                _longStarted = true;
                while (true)
                {
                    await Task.Yield();
                    if (_intStarted)
                        return 100;
                }
            }

            [Factory]
            string M3(int i, long l)
            {
                return (i + l).ToString();
            }
        }

        [Fact]
        public async Task TestResolvesInParallel()
        {
            await new TestResolvesInParallelContainer().RunAsync(x => Assert.Equal("142", x));
        }

        public partial class InProgressTasksAreDisposedOfWhenResolutionFailsContainer : IAsyncContainer<string>
        {
            public bool IntDisposed { get; private set; } = false;
            public bool LongDisposed { get; private set; } = false;

            [Factory]
            async Task<ActionDisposable<int>> M1()
            {
                await Task.Yield();
                return new ActionDisposable<int>(() => IntDisposed = true);
            }

            [Factory]
            async Task<ActionDisposable<long>> M2()
            {
                await Task.Yield();
                return new ActionDisposable<long>(() => LongDisposed = true);
            }

            [Factory]
            float M3() => throw new Exception();

            [Factory]
            string M4(ActionDisposable<int> i, ActionDisposable<long> l, float f) => "";
        }

        [Fact]
        public async Task TestInProgressTasksAreDisposedOfWhenResolutionFails()
        {
            var container = new InProgressTasksAreDisposedOfWhenResolutionFailsContainer();
            await Assert.ThrowsAsync<Exception>(() => container.ResolveAsync().AsTask());
            Assert.True(container.IntDisposed);
            Assert.True(container.LongDisposed);
        }
    }
}
