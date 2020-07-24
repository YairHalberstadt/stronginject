using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace StrongInject
{
    public interface IAsyncContainer<T>
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        ValueTask<TResult> RunAsync<TResult, TParam>(Func<T, TParam, ValueTask<TResult>> func, TParam param);
    }
}
