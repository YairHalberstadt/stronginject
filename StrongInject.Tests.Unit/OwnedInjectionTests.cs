using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace StrongInject.Generator.Tests.Unit
{
    public class OwnedInjectionTests : TestBase
    {
        public OwnedInjectionTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact]
        public void InjectParameterFuncOfOwnedOfDisposableDependencyWithDisposableDependency()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A)), Register(typeof(B)), Register(typeof(C))]
public partial class Container : IContainer<A> { }

public record A(Func<Owned<B>> B);
public record B(C C) : IDisposable { public void Dispose() { } }
public record C : IDisposable { public void Dispose() { } };
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::StrongInject.Owned<global::B>> func_0_1;
        global::A a_0_0;
        func_0_1 = null;
        func_0_1 = () =>
        {
            global::StrongInject.Owned<global::B> owned_1_0;
            global::StrongInject.Owned<global::B> CreateOwnedB_1()
            {
                global::C c_1_1;
                global::B b_1_0;
                c_1_1 = new global::C();
                try
                {
                    b_1_0 = new global::B(C: c_1_1);
                }
                catch
                {
                    ((global::System.IDisposable)c_1_1).Dispose();
                    throw;
                }

                return new global::StrongInject.Owned<global::B>(b_1_0, () =>
                {
                    ((global::System.IDisposable)b_1_0).Dispose();
                    ((global::System.IDisposable)c_1_1).Dispose();
                });
            }

            owned_1_0 = CreateOwnedB_1();
            return owned_1_0;
        };
        a_0_0 = new global::A(B: func_0_1);
        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::StrongInject.Owned<global::B>> func_0_1;
        global::A a_0_0;
        func_0_1 = null;
        func_0_1 = () =>
        {
            global::StrongInject.Owned<global::B> owned_1_0;
            global::StrongInject.Owned<global::B> CreateOwnedB_1()
            {
                global::C c_1_1;
                global::B b_1_0;
                c_1_1 = new global::C();
                try
                {
                    b_1_0 = new global::B(C: c_1_1);
                }
                catch
                {
                    ((global::System.IDisposable)c_1_1).Dispose();
                    throw;
                }

                return new global::StrongInject.Owned<global::B>(b_1_0, () =>
                {
                    ((global::System.IDisposable)b_1_0).Dispose();
                    ((global::System.IDisposable)c_1_1).Dispose();
                });
            }

            owned_1_0 = CreateOwnedB_1();
            return owned_1_0;
        };
        a_0_0 = new global::A(B: func_0_1);
        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void InjectParameterOwnedOfDisposableDependencyWithDisposableDependency()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A)), Register(typeof(B)), Register(typeof(C))]
public partial class Container : IContainer<A> { }

public record A(Owned<B> B);
public record B(C C) : IDisposable { public void Dispose() { } }
public record C : IDisposable { public void Dispose() { } };
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::B> owned_0_1;
        global::A a_0_0;
        global::StrongInject.Owned<global::B> CreateOwnedB_2()
        {
            global::C c_0_1;
            global::B b_0_0;
            c_0_1 = new global::C();
            try
            {
                b_0_0 = new global::B(C: c_0_1);
            }
            catch
            {
                ((global::System.IDisposable)c_0_1).Dispose();
                throw;
            }

            return new global::StrongInject.Owned<global::B>(b_0_0, () =>
            {
                ((global::System.IDisposable)b_0_0).Dispose();
                ((global::System.IDisposable)c_0_1).Dispose();
            });
        }

        owned_0_1 = CreateOwnedB_2();
        a_0_0 = new global::A(B: owned_0_1);
        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::B> owned_0_1;
        global::A a_0_0;
        global::StrongInject.Owned<global::B> CreateOwnedB_2()
        {
            global::C c_0_1;
            global::B b_0_0;
            c_0_1 = new global::C();
            try
            {
                b_0_0 = new global::B(C: c_0_1);
            }
            catch
            {
                ((global::System.IDisposable)c_0_1).Dispose();
                throw;
            }

            return new global::StrongInject.Owned<global::B>(b_0_0, () =>
            {
                ((global::System.IDisposable)b_0_0).Dispose();
                ((global::System.IDisposable)c_0_1).Dispose();
            });
        }

        owned_0_1 = CreateOwnedB_2();
        a_0_0 = new global::A(B: owned_0_1);
        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void InjectParameterOwnedOfDisposableDependencyWithSharedDisposableDependency()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A)), Register(typeof(B)), Register(typeof(C))]
public partial class Container : IContainer<A> { }

public record A(Owned<B> B, C C);
public record B(C C) : IDisposable { public void Dispose() { } }
public record C : IDisposable { public void Dispose() { } };
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::B> owned_0_1;
        global::C c_0_3;
        global::A a_0_0;
        global::StrongInject.Owned<global::B> CreateOwnedB_2()
        {
            global::C c_0_1;
            global::B b_0_0;
            c_0_1 = new global::C();
            try
            {
                b_0_0 = new global::B(C: c_0_1);
            }
            catch
            {
                ((global::System.IDisposable)c_0_1).Dispose();
                throw;
            }

            return new global::StrongInject.Owned<global::B>(b_0_0, () =>
            {
                ((global::System.IDisposable)b_0_0).Dispose();
                ((global::System.IDisposable)c_0_1).Dispose();
            });
        }

        owned_0_1 = CreateOwnedB_2();
        c_0_3 = new global::C();
        try
        {
            a_0_0 = new global::A(B: owned_0_1, C: c_0_3);
        }
        catch
        {
            ((global::System.IDisposable)c_0_3).Dispose();
            throw;
        }

        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
            ((global::System.IDisposable)c_0_3).Dispose();
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::B> owned_0_1;
        global::C c_0_3;
        global::A a_0_0;
        global::StrongInject.Owned<global::B> CreateOwnedB_2()
        {
            global::C c_0_1;
            global::B b_0_0;
            c_0_1 = new global::C();
            try
            {
                b_0_0 = new global::B(C: c_0_1);
            }
            catch
            {
                ((global::System.IDisposable)c_0_1).Dispose();
                throw;
            }

            return new global::StrongInject.Owned<global::B>(b_0_0, () =>
            {
                ((global::System.IDisposable)b_0_0).Dispose();
                ((global::System.IDisposable)c_0_1).Dispose();
            });
        }

        owned_0_1 = CreateOwnedB_2();
        c_0_3 = new global::C();
        try
        {
            a_0_0 = new global::A(B: owned_0_1, C: c_0_3);
        }
        catch
        {
            ((global::System.IDisposable)c_0_3).Dispose();
            throw;
        }

        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
            ((global::System.IDisposable)c_0_3).Dispose();
        });
    }
}");
        }

        [Fact]
        public void InjectParameterOwnedOfNonDisposableDependencyWithDisposableDependency()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A)), Register(typeof(B)), Register(typeof(C))]
public partial class Container : IContainer<A> { }

public record A(Owned<B> B);
public record B(C C);
public record C : IDisposable { public void Dispose() { } };
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::B> owned_0_1;
        global::A a_0_0;
        global::StrongInject.Owned<global::B> CreateOwnedB_2()
        {
            global::C c_0_1;
            global::B b_0_0;
            c_0_1 = new global::C();
            try
            {
                b_0_0 = new global::B(C: c_0_1);
            }
            catch
            {
                ((global::System.IDisposable)c_0_1).Dispose();
                throw;
            }

            return new global::StrongInject.Owned<global::B>(b_0_0, () =>
            {
                ((global::System.IDisposable)c_0_1).Dispose();
            });
        }

        owned_0_1 = CreateOwnedB_2();
        a_0_0 = new global::A(B: owned_0_1);
        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::B> owned_0_1;
        global::A a_0_0;
        global::StrongInject.Owned<global::B> CreateOwnedB_2()
        {
            global::C c_0_1;
            global::B b_0_0;
            c_0_1 = new global::C();
            try
            {
                b_0_0 = new global::B(C: c_0_1);
            }
            catch
            {
                ((global::System.IDisposable)c_0_1).Dispose();
                throw;
            }

            return new global::StrongInject.Owned<global::B>(b_0_0, () =>
            {
                ((global::System.IDisposable)c_0_1).Dispose();
            });
        }

        owned_0_1 = CreateOwnedB_2();
        a_0_0 = new global::A(B: owned_0_1);
        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void InjectDifferentOwnedTypesWithSameName()
        {
            string userSource = @"
using StrongInject;

[Register(typeof(A)), Register(typeof(N1.B)), Register(typeof(N2.B))]
public partial class Container : IContainer<A> { }

public record A(Owned<N1.B> N1B, Owned<N2.B> N2B);
namespace N1 { public record B; }
namespace N2 { public record B; }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::N1.B> owned_0_1;
        global::StrongInject.Owned<global::N2.B> owned_0_3;
        global::A a_0_0;
        global::StrongInject.Owned<global::N1.B> CreateOwnedB_2()
        {
            global::N1.B b_0_0;
            b_0_0 = new global::N1.B();
            return new global::StrongInject.Owned<global::N1.B>(b_0_0, () =>
            {
            });
        }

        owned_0_1 = CreateOwnedB_2();
        global::StrongInject.Owned<global::N2.B> CreateOwnedB_4()
        {
            global::N2.B b_0_0;
            b_0_0 = new global::N2.B();
            return new global::StrongInject.Owned<global::N2.B>(b_0_0, () =>
            {
            });
        }

        owned_0_3 = CreateOwnedB_4();
        a_0_0 = new global::A(N1B: owned_0_1, N2B: owned_0_3);
        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::N1.B> owned_0_1;
        global::StrongInject.Owned<global::N2.B> owned_0_3;
        global::A a_0_0;
        global::StrongInject.Owned<global::N1.B> CreateOwnedB_2()
        {
            global::N1.B b_0_0;
            b_0_0 = new global::N1.B();
            return new global::StrongInject.Owned<global::N1.B>(b_0_0, () =>
            {
            });
        }

        owned_0_1 = CreateOwnedB_2();
        global::StrongInject.Owned<global::N2.B> CreateOwnedB_4()
        {
            global::N2.B b_0_0;
            b_0_0 = new global::N2.B();
            return new global::StrongInject.Owned<global::N2.B>(b_0_0, () =>
            {
            });
        }

        owned_0_3 = CreateOwnedB_4();
        a_0_0 = new global::A(N1B: owned_0_1, N2B: owned_0_3);
        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
        });
    }
}");
        }

        // Demonstrates an especially useless thing to do as currently producing working code with no StrongInject diagnostics.
        [Fact]
        public void InjectOwnedOfOwned()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A)), Register(typeof(B))]
public partial class Container : IContainer<A> { }

public record A(Owned<Owned<B>> B);
public record B : IDisposable { public void Dispose() { } };
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::StrongInject.Owned<global::B>> owned_0_1;
        global::A a_0_0;
        global::StrongInject.Owned<global::StrongInject.Owned<global::B>> CreateOwnedOwned_2()
        {
            global::StrongInject.Owned<global::B> owned_0_0;
            global::StrongInject.Owned<global::B> CreateOwnedB_1()
            {
                global::B b_0_0;
                b_0_0 = new global::B();
                return new global::StrongInject.Owned<global::B>(b_0_0, () =>
                {
                    ((global::System.IDisposable)b_0_0).Dispose();
                });
            }

            owned_0_0 = CreateOwnedB_1();
            return new global::StrongInject.Owned<global::StrongInject.Owned<global::B>>(owned_0_0, () =>
            {
            });
        }

        owned_0_1 = CreateOwnedOwned_2();
        a_0_0 = new global::A(B: owned_0_1);
        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::StrongInject.Owned<global::B>> owned_0_1;
        global::A a_0_0;
        global::StrongInject.Owned<global::StrongInject.Owned<global::B>> CreateOwnedOwned_2()
        {
            global::StrongInject.Owned<global::B> owned_0_0;
            global::StrongInject.Owned<global::B> CreateOwnedB_1()
            {
                global::B b_0_0;
                b_0_0 = new global::B();
                return new global::StrongInject.Owned<global::B>(b_0_0, () =>
                {
                    ((global::System.IDisposable)b_0_0).Dispose();
                });
            }

            owned_0_0 = CreateOwnedB_1();
            return new global::StrongInject.Owned<global::StrongInject.Owned<global::B>>(owned_0_0, () =>
            {
            });
        }

        owned_0_1 = CreateOwnedOwned_2();
        a_0_0 = new global::A(B: owned_0_1);
        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
        });
    }
}");
        }

        // Demonstrates another especially useless thing to do as currently producing working code with no StrongInject diagnostics.
        [Fact]
        public void ResolveOwned()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A))]
public partial class Container : IContainer<Owned<A>> { }

public record A : IDisposable { public void Dispose() { } };
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::StrongInject.Owned<global::A>>.Run<TResult, TParam>(global::System.Func<global::StrongInject.Owned<global::A>, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::A> owned_0_0;
        global::StrongInject.Owned<global::A> CreateOwnedA_1()
        {
            global::A a_0_0;
            a_0_0 = new global::A();
            return new global::StrongInject.Owned<global::A>(a_0_0, () =>
            {
                ((global::System.IDisposable)a_0_0).Dispose();
            });
        }

        owned_0_0 = CreateOwnedA_1();
        TResult result;
        try
        {
            result = func(owned_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::StrongInject.Owned<global::A>> global::StrongInject.IContainer<global::StrongInject.Owned<global::A>>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::A> owned_0_0;
        global::StrongInject.Owned<global::A> CreateOwnedA_1()
        {
            global::A a_0_0;
            a_0_0 = new global::A();
            return new global::StrongInject.Owned<global::A>(a_0_0, () =>
            {
                ((global::System.IDisposable)a_0_0).Dispose();
            });
        }

        owned_0_0 = CreateOwnedA_1();
        return new global::StrongInject.Owned<global::StrongInject.Owned<global::A>>(owned_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void OwnedCanBeManuallyProvidedViaFactory()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A)), Register(typeof(B))]
public partial class Container : IContainer<A>
{
    [Factory]
    public static Owned<B> CreateOwnedManually() => throw new NotImplementedException();
}

public record A(Owned<B> B);
public record B : IDisposable { public void Dispose() { } }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::B> owned_0_1;
        global::A a_0_0;
        owned_0_1 = global::Container.CreateOwnedManually();
        try
        {
            a_0_0 = new global::A(B: owned_0_1);
        }
        catch
        {
            ((global::System.IDisposable)owned_0_1).Dispose();
            throw;
        }

        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
            ((global::System.IDisposable)owned_0_1).Dispose();
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::B> owned_0_1;
        global::A a_0_0;
        owned_0_1 = global::Container.CreateOwnedManually();
        try
        {
            a_0_0 = new global::A(B: owned_0_1);
        }
        catch
        {
            ((global::System.IDisposable)owned_0_1).Dispose();
            throw;
        }

        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
            ((global::System.IDisposable)owned_0_1).Dispose();
        });
    }
}");
        }

        // Demonstrates another especially useless thing to do as currently producing working code with no StrongInject diagnostics.
        // (There's no way to provide a semantically correct dispose action if the contained instance or any of its dependencies are disposable.)
        [Fact]
        public void OwnedCanBeRegisteredExplicitly()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A)), Register(typeof(B)), Register(typeof(Owned<>))]
public partial class Container : IContainer<A>
{
    [Instance]
    private static readonly Action NoOp = () => { };
}

public record A(Owned<B> B);
public record B : IDisposable { public void Dispose() { } }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B b_0_2;
        global::System.Action action_0_3;
        global::StrongInject.Owned<global::B> owned_0_1;
        global::A a_0_0;
        b_0_2 = new global::B();
        try
        {
            action_0_3 = global::Container.NoOp;
            owned_0_1 = new global::StrongInject.Owned<global::B>(value: b_0_2, dispose: action_0_3);
            try
            {
                a_0_0 = new global::A(B: owned_0_1);
            }
            catch
            {
                ((global::System.IDisposable)owned_0_1).Dispose();
                throw;
            }
        }
        catch
        {
            ((global::System.IDisposable)b_0_2).Dispose();
            throw;
        }

        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
            ((global::System.IDisposable)owned_0_1).Dispose();
            ((global::System.IDisposable)b_0_2).Dispose();
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B b_0_2;
        global::System.Action action_0_3;
        global::StrongInject.Owned<global::B> owned_0_1;
        global::A a_0_0;
        b_0_2 = new global::B();
        try
        {
            action_0_3 = global::Container.NoOp;
            owned_0_1 = new global::StrongInject.Owned<global::B>(value: b_0_2, dispose: action_0_3);
            try
            {
                a_0_0 = new global::A(B: owned_0_1);
            }
            catch
            {
                ((global::System.IDisposable)owned_0_1).Dispose();
                throw;
            }
        }
        catch
        {
            ((global::System.IDisposable)b_0_2).Dispose();
            throw;
        }

        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
            ((global::System.IDisposable)owned_0_1).Dispose();
            ((global::System.IDisposable)b_0_2).Dispose();
        });
    }
}");
        }

        [Fact]
        public void OwnedCanBeDecorated()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A)), Register(typeof(B))]
public partial class Container : IContainer<A>
{
    [DecoratorFactory]
    public static Owned<T> DecorateOwned<T>(Owned<T> inner) => throw new NotImplementedException();
}

public record A(Owned<B> B);
public record B : IDisposable { public void Dispose() { } }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::B> owned_0_2;
        global::StrongInject.Owned<global::B> owned_0_1;
        global::A a_0_0;
        global::StrongInject.Owned<global::B> CreateOwnedB_3()
        {
            global::B b_0_0;
            b_0_0 = new global::B();
            return new global::StrongInject.Owned<global::B>(b_0_0, () =>
            {
                ((global::System.IDisposable)b_0_0).Dispose();
            });
        }

        owned_0_2 = CreateOwnedB_3();
        owned_0_1 = global::Container.DecorateOwned<global::B>(inner: owned_0_2);
        a_0_0 = new global::A(B: owned_0_1);
        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::B> owned_0_2;
        global::StrongInject.Owned<global::B> owned_0_1;
        global::A a_0_0;
        global::StrongInject.Owned<global::B> CreateOwnedB_3()
        {
            global::B b_0_0;
            b_0_0 = new global::B();
            return new global::StrongInject.Owned<global::B>(b_0_0, () =>
            {
                ((global::System.IDisposable)b_0_0).Dispose();
            });
        }

        owned_0_2 = CreateOwnedB_3();
        owned_0_1 = global::Container.DecorateOwned<global::B>(inner: owned_0_2);
        a_0_0 = new global::A(B: owned_0_1);
        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void TryStatementShouldNotBeAddedForOwnedCreation()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A)), Register(typeof(B)), Register(typeof(C)), Register(typeof(D))]
public partial class Container : IContainer<A> { }

public record A(B B, Owned<C> C, D D);
public record B : IDisposable { public void Dispose() { } }
public record C;
public record D : IDisposable { public void Dispose() { } }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B b_0_1;
        global::StrongInject.Owned<global::C> owned_0_2;
        global::D d_0_4;
        global::A a_0_0;
        b_0_1 = new global::B();
        try
        {
            global::StrongInject.Owned<global::C> CreateOwnedC_3()
            {
                global::C c_0_0;
                c_0_0 = new global::C();
                return new global::StrongInject.Owned<global::C>(c_0_0, () =>
                {
                });
            }

            owned_0_2 = CreateOwnedC_3();
            d_0_4 = new global::D();
            try
            {
                a_0_0 = new global::A(B: b_0_1, C: owned_0_2, D: d_0_4);
            }
            catch
            {
                ((global::System.IDisposable)d_0_4).Dispose();
                throw;
            }
        }
        catch
        {
            ((global::System.IDisposable)b_0_1).Dispose();
            throw;
        }

        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
            ((global::System.IDisposable)d_0_4).Dispose();
            ((global::System.IDisposable)b_0_1).Dispose();
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::B b_0_1;
        global::StrongInject.Owned<global::C> owned_0_2;
        global::D d_0_4;
        global::A a_0_0;
        b_0_1 = new global::B();
        try
        {
            global::StrongInject.Owned<global::C> CreateOwnedC_3()
            {
                global::C c_0_0;
                c_0_0 = new global::C();
                return new global::StrongInject.Owned<global::C>(c_0_0, () =>
                {
                });
            }

            owned_0_2 = CreateOwnedC_3();
            d_0_4 = new global::D();
            try
            {
                a_0_0 = new global::A(B: b_0_1, C: owned_0_2, D: d_0_4);
            }
            catch
            {
                ((global::System.IDisposable)d_0_4).Dispose();
                throw;
            }
        }
        catch
        {
            ((global::System.IDisposable)b_0_1).Dispose();
            throw;
        }

        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
            ((global::System.IDisposable)d_0_4).Dispose();
            ((global::System.IDisposable)b_0_1).Dispose();
        });
    }
}");
        }

        [Fact]
        public void InjectParameterFuncOfAsyncOwnedOfDisposableDependency()
        {
            string userSource = @"
using System;
using System.Threading.Tasks;
using StrongInject;

[Register(typeof(A)), Register(typeof(B))]
public partial class Container : IContainer<A> { }

public record A(Func<AsyncOwned<B>> B);
public record B : IAsyncDisposable { public ValueTask DisposeAsync() => default; }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::StrongInject.AsyncOwned<global::B>> func_0_1;
        global::A a_0_0;
        func_0_1 = null;
        func_0_1 = () =>
        {
            global::StrongInject.AsyncOwned<global::B> asyncOwned_1_0;
            global::StrongInject.AsyncOwned<global::B> CreateAsyncOwnedB_1()
            {
                global::B b_1_0;
                b_1_0 = new global::B();
                return new global::StrongInject.AsyncOwned<global::B>(b_1_0, async () =>
                {
                    await ((global::System.IAsyncDisposable)b_1_0).DisposeAsync();
                });
            }

            asyncOwned_1_0 = CreateAsyncOwnedB_1();
            return asyncOwned_1_0;
        };
        a_0_0 = new global::A(B: func_0_1);
        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::StrongInject.AsyncOwned<global::B>> func_0_1;
        global::A a_0_0;
        func_0_1 = null;
        func_0_1 = () =>
        {
            global::StrongInject.AsyncOwned<global::B> asyncOwned_1_0;
            global::StrongInject.AsyncOwned<global::B> CreateAsyncOwnedB_1()
            {
                global::B b_1_0;
                b_1_0 = new global::B();
                return new global::StrongInject.AsyncOwned<global::B>(b_1_0, async () =>
                {
                    await ((global::System.IAsyncDisposable)b_1_0).DisposeAsync();
                });
            }

            asyncOwned_1_0 = CreateAsyncOwnedB_1();
            return asyncOwned_1_0;
        };
        a_0_0 = new global::A(B: func_0_1);
        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void InjectParameterAsyncOwnedOfDisposableDependency()
        {
            string userSource = @"
using System;
using System.Threading.Tasks;
using StrongInject;

[Register(typeof(A)), Register(typeof(B))]
public partial class Container : IContainer<A> { }

public record A(AsyncOwned<B> B);
public record B : IAsyncDisposable { public ValueTask DisposeAsync() => default; }
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.AsyncOwned<global::B> asyncOwned_0_1;
        global::A a_0_0;
        global::StrongInject.AsyncOwned<global::B> CreateAsyncOwnedB_2()
        {
            global::B b_0_0;
            b_0_0 = new global::B();
            return new global::StrongInject.AsyncOwned<global::B>(b_0_0, async () =>
            {
                await ((global::System.IAsyncDisposable)b_0_0).DisposeAsync();
            });
        }

        asyncOwned_0_1 = CreateAsyncOwnedB_2();
        a_0_0 = new global::A(B: asyncOwned_0_1);
        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.AsyncOwned<global::B> asyncOwned_0_1;
        global::A a_0_0;
        global::StrongInject.AsyncOwned<global::B> CreateAsyncOwnedB_2()
        {
            global::B b_0_0;
            b_0_0 = new global::B();
            return new global::StrongInject.AsyncOwned<global::B>(b_0_0, async () =>
            {
                await ((global::System.IAsyncDisposable)b_0_0).DisposeAsync();
            });
        }

        asyncOwned_0_1 = CreateAsyncOwnedB_2();
        a_0_0 = new global::A(B: asyncOwned_0_1);
        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void InjectParameterFuncOfOwnedWithAsyncInitializationOfDependency()
        {
            string userSource = @"
using System;
using System.Threading.Tasks;
using StrongInject;

[Register(typeof(A)), Register(typeof(B))]
public partial class Container : IContainer<A> { }

public record A(Func<Task<Owned<B>>> B);
public record B : IRequiresAsyncInitialization, IDisposable
{
    public ValueTask InitializeAsync() => default;
    public void Dispose() { }
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::System.Threading.Tasks.Task<global::StrongInject.Owned<global::B>>> func_0_1;
        global::A a_0_0;
        func_0_1 = null;
        func_0_1 = async () =>
        {
            global::StrongInject.Owned<global::B> owned_1_0;
            async global::System.Threading.Tasks.ValueTask<global::StrongInject.Owned<global::B>> CreateOwnedB_1()
            {
                global::B b_1_0;
                global::System.Threading.Tasks.ValueTask b_1_1;
                var hasAwaitStarted_b_1_1 = false;
                b_1_0 = new global::B();
                try
                {
                    b_1_1 = ((global::StrongInject.IRequiresAsyncInitialization)b_1_0).InitializeAsync();
                    try
                    {
                        hasAwaitStarted_b_1_1 = true;
                        await b_1_1;
                    }
                    catch
                    {
                        if (!hasAwaitStarted_b_1_1)
                        {
                            _ = b_1_1.AsTask().ContinueWith(failedTask => _ = failedTask.Exception, global::System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
                        }

                        throw;
                    }
                }
                catch
                {
                    ((global::System.IDisposable)b_1_0).Dispose();
                    throw;
                }

                return new global::StrongInject.Owned<global::B>(b_1_0, () =>
                {
                    ((global::System.IDisposable)b_1_0).Dispose();
                });
            }

            owned_1_0 = await CreateOwnedB_1();
            return owned_1_0;
        };
        a_0_0 = new global::A(B: func_0_1);
        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::System.Threading.Tasks.Task<global::StrongInject.Owned<global::B>>> func_0_1;
        global::A a_0_0;
        func_0_1 = null;
        func_0_1 = async () =>
        {
            global::StrongInject.Owned<global::B> owned_1_0;
            async global::System.Threading.Tasks.ValueTask<global::StrongInject.Owned<global::B>> CreateOwnedB_1()
            {
                global::B b_1_0;
                global::System.Threading.Tasks.ValueTask b_1_1;
                var hasAwaitStarted_b_1_1 = false;
                b_1_0 = new global::B();
                try
                {
                    b_1_1 = ((global::StrongInject.IRequiresAsyncInitialization)b_1_0).InitializeAsync();
                    try
                    {
                        hasAwaitStarted_b_1_1 = true;
                        await b_1_1;
                    }
                    catch
                    {
                        if (!hasAwaitStarted_b_1_1)
                        {
                            _ = b_1_1.AsTask().ContinueWith(failedTask => _ = failedTask.Exception, global::System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
                        }

                        throw;
                    }
                }
                catch
                {
                    ((global::System.IDisposable)b_1_0).Dispose();
                    throw;
                }

                return new global::StrongInject.Owned<global::B>(b_1_0, () =>
                {
                    ((global::System.IDisposable)b_1_0).Dispose();
                });
            }

            owned_1_0 = await CreateOwnedB_1();
            return owned_1_0;
        };
        a_0_0 = new global::A(B: func_0_1);
        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void InjectParameterFuncOfAsyncOwnedWithAsyncInitializationOfDependency()
        {
            string userSource = @"
using System;
using System.Threading.Tasks;
using StrongInject;

[Register(typeof(A)), Register(typeof(B))]
public partial class Container : IContainer<A> { }

public record A(Func<Task<AsyncOwned<B>>> B);
public record B : IRequiresAsyncInitialization, IAsyncDisposable
{
    public ValueTask InitializeAsync() => default;
    public ValueTask DisposeAsync() => default;
}
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::System.Threading.Tasks.Task<global::StrongInject.AsyncOwned<global::B>>> func_0_1;
        global::A a_0_0;
        func_0_1 = null;
        func_0_1 = async () =>
        {
            global::StrongInject.AsyncOwned<global::B> asyncOwned_1_0;
            async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::B>> CreateAsyncOwnedB_1()
            {
                global::B b_1_0;
                global::System.Threading.Tasks.ValueTask b_1_1;
                var hasAwaitStarted_b_1_1 = false;
                b_1_0 = new global::B();
                try
                {
                    b_1_1 = ((global::StrongInject.IRequiresAsyncInitialization)b_1_0).InitializeAsync();
                    try
                    {
                        hasAwaitStarted_b_1_1 = true;
                        await b_1_1;
                    }
                    catch
                    {
                        if (!hasAwaitStarted_b_1_1)
                        {
                            _ = b_1_1.AsTask().ContinueWith(failedTask => _ = failedTask.Exception, global::System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
                        }

                        throw;
                    }
                }
                catch
                {
                    await ((global::System.IAsyncDisposable)b_1_0).DisposeAsync();
                    throw;
                }

                return new global::StrongInject.AsyncOwned<global::B>(b_1_0, async () =>
                {
                    await ((global::System.IAsyncDisposable)b_1_0).DisposeAsync();
                });
            }

            asyncOwned_1_0 = await CreateAsyncOwnedB_1();
            return asyncOwned_1_0;
        };
        a_0_0 = new global::A(B: func_0_1);
        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::System.Threading.Tasks.Task<global::StrongInject.AsyncOwned<global::B>>> func_0_1;
        global::A a_0_0;
        func_0_1 = null;
        func_0_1 = async () =>
        {
            global::StrongInject.AsyncOwned<global::B> asyncOwned_1_0;
            async global::System.Threading.Tasks.ValueTask<global::StrongInject.AsyncOwned<global::B>> CreateAsyncOwnedB_1()
            {
                global::B b_1_0;
                global::System.Threading.Tasks.ValueTask b_1_1;
                var hasAwaitStarted_b_1_1 = false;
                b_1_0 = new global::B();
                try
                {
                    b_1_1 = ((global::StrongInject.IRequiresAsyncInitialization)b_1_0).InitializeAsync();
                    try
                    {
                        hasAwaitStarted_b_1_1 = true;
                        await b_1_1;
                    }
                    catch
                    {
                        if (!hasAwaitStarted_b_1_1)
                        {
                            _ = b_1_1.AsTask().ContinueWith(failedTask => _ = failedTask.Exception, global::System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
                        }

                        throw;
                    }
                }
                catch
                {
                    await ((global::System.IAsyncDisposable)b_1_0).DisposeAsync();
                    throw;
                }

                return new global::StrongInject.AsyncOwned<global::B>(b_1_0, async () =>
                {
                    await ((global::System.IAsyncDisposable)b_1_0).DisposeAsync();
                });
            }

            asyncOwned_1_0 = await CreateAsyncOwnedB_1();
            return asyncOwned_1_0;
        };
        a_0_0 = new global::A(B: func_0_1);
        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void LocalFunctionsShouldNotBeGeneratedMoreTimesThanNecessary()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A)), Register(typeof(B)), Register(typeof(C))]
public partial class Container : IContainer<A> { }

public record A(B B, Owned<C> C1, Owned<C> C2);
public record B(Owned<C> C1, Owned<C> C2) : IDisposable { public void Dispose() { } }
public record C : IDisposable { public void Dispose() { } };
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::C> owned_0_2;
        global::StrongInject.Owned<global::C> owned_0_4;
        global::B b_0_1;
        global::StrongInject.Owned<global::C> owned_0_5;
        global::StrongInject.Owned<global::C> owned_0_6;
        global::A a_0_0;
        global::StrongInject.Owned<global::C> CreateOwnedC_3()
        {
            global::C c_0_0;
            c_0_0 = new global::C();
            return new global::StrongInject.Owned<global::C>(c_0_0, () =>
            {
                ((global::System.IDisposable)c_0_0).Dispose();
            });
        }

        owned_0_2 = CreateOwnedC_3();
        owned_0_4 = CreateOwnedC_3();
        b_0_1 = new global::B(C1: owned_0_2, C2: owned_0_4);
        try
        {
            owned_0_5 = CreateOwnedC_3();
            owned_0_6 = CreateOwnedC_3();
            a_0_0 = new global::A(B: b_0_1, C1: owned_0_5, C2: owned_0_6);
        }
        catch
        {
            ((global::System.IDisposable)b_0_1).Dispose();
            throw;
        }

        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
            ((global::System.IDisposable)b_0_1).Dispose();
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::StrongInject.Owned<global::C> owned_0_2;
        global::StrongInject.Owned<global::C> owned_0_4;
        global::B b_0_1;
        global::StrongInject.Owned<global::C> owned_0_5;
        global::StrongInject.Owned<global::C> owned_0_6;
        global::A a_0_0;
        global::StrongInject.Owned<global::C> CreateOwnedC_3()
        {
            global::C c_0_0;
            c_0_0 = new global::C();
            return new global::StrongInject.Owned<global::C>(c_0_0, () =>
            {
                ((global::System.IDisposable)c_0_0).Dispose();
            });
        }

        owned_0_2 = CreateOwnedC_3();
        owned_0_4 = CreateOwnedC_3();
        b_0_1 = new global::B(C1: owned_0_2, C2: owned_0_4);
        try
        {
            owned_0_5 = CreateOwnedC_3();
            owned_0_6 = CreateOwnedC_3();
            a_0_0 = new global::A(B: b_0_1, C1: owned_0_5, C2: owned_0_6);
        }
        catch
        {
            ((global::System.IDisposable)b_0_1).Dispose();
            throw;
        }

        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
            ((global::System.IDisposable)b_0_1).Dispose();
        });
    }
}");
        }

        [Fact]
        public void LocalFunctionsShouldBeGeneratedSeparatelyWhenUsingDelegateParameter()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A)), Register(typeof(B)), Register(typeof(C))]
public partial class Container : IContainer<A> { }

public record A(Func<C, Owned<B>> B1, Owned<B> B2);
public record B(C C);
public record C : IDisposable { public void Dispose() { } };
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::C, global::StrongInject.Owned<global::B>> func_0_1;
        global::StrongInject.Owned<global::B> owned_0_2;
        global::A a_0_0;
        func_0_1 = null;
        func_0_1 = (param1_0) =>
        {
            global::StrongInject.Owned<global::B> owned_1_0;
            global::StrongInject.Owned<global::B> CreateOwnedB_1()
            {
                global::B b_1_0;
                b_1_0 = new global::B(C: param1_0);
                return new global::StrongInject.Owned<global::B>(b_1_0, () =>
                {
                });
            }

            owned_1_0 = CreateOwnedB_1();
            return owned_1_0;
        };
        global::StrongInject.Owned<global::B> CreateOwnedB_3()
        {
            global::C c_0_1;
            global::B b_0_0;
            c_0_1 = new global::C();
            try
            {
                b_0_0 = new global::B(C: c_0_1);
            }
            catch
            {
                ((global::System.IDisposable)c_0_1).Dispose();
                throw;
            }

            return new global::StrongInject.Owned<global::B>(b_0_0, () =>
            {
                ((global::System.IDisposable)c_0_1).Dispose();
            });
        }

        owned_0_2 = CreateOwnedB_3();
        a_0_0 = new global::A(B1: func_0_1, B2: owned_0_2);
        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::C, global::StrongInject.Owned<global::B>> func_0_1;
        global::StrongInject.Owned<global::B> owned_0_2;
        global::A a_0_0;
        func_0_1 = null;
        func_0_1 = (param1_0) =>
        {
            global::StrongInject.Owned<global::B> owned_1_0;
            global::StrongInject.Owned<global::B> CreateOwnedB_1()
            {
                global::B b_1_0;
                b_1_0 = new global::B(C: param1_0);
                return new global::StrongInject.Owned<global::B>(b_1_0, () =>
                {
                });
            }

            owned_1_0 = CreateOwnedB_1();
            return owned_1_0;
        };
        global::StrongInject.Owned<global::B> CreateOwnedB_3()
        {
            global::C c_0_1;
            global::B b_0_0;
            c_0_1 = new global::C();
            try
            {
                b_0_0 = new global::B(C: c_0_1);
            }
            catch
            {
                ((global::System.IDisposable)c_0_1).Dispose();
                throw;
            }

            return new global::StrongInject.Owned<global::B>(b_0_0, () =>
            {
                ((global::System.IDisposable)c_0_1).Dispose();
            });
        }

        owned_0_2 = CreateOwnedB_3();
        a_0_0 = new global::A(B1: func_0_1, B2: owned_0_2);
        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void AsyncOwnedIsRequiredForAsyncDisposables()
        {
            string userSource = @"
using System;
using System.Threading.Tasks;
using StrongInject;

[Register(typeof(A)), Register(typeof(B))]
public partial class Container : IAsyncContainer<A> { }

public record A(Func<Owned<B>> B);
public record B : IAsyncDisposable { public ValueTask DisposeAsync() => default; }
";
            _ = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out _);
            generatorDiagnostics.Verify(
                // (7,22): Warning SI1301: Cannot call asynchronous dispose for 'B' using 'StrongInject.Owned<B>'; use 'StrongInject.AsyncOwned<B>' instead
                // Container
                new DiagnosticResult("SI1301", @"Container", DiagnosticSeverity.Warning).WithLocation(7, 22));
        }

        [Fact]
        public void AsyncContainerIsRequiredForOwnedOfAsyncInitializedDependency()
        {
            string userSource = @"
using System;
using System.Threading.Tasks;
using StrongInject;

[Register(typeof(A)), Register(typeof(B))]
public partial class Container : IContainer<A> { }

public record A(Owned<B> B);
public record B : IRequiresAsyncInitialization { public ValueTask InitializeAsync() => default; }
";
            _ = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out _);
            generatorDiagnostics.Verify(
                // (7,22): Error SI0103: Error while resolving dependencies for 'A': 'B' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(7, 22));
        }

        [Fact]
        public void AsyncContainerIsRequiredForAsyncOwnedOfAsyncInitializedDependency()
        {
            string userSource = @"
using System;
using System.Threading.Tasks;
using StrongInject;

[Register(typeof(A)), Register(typeof(B))]
public partial class Container : IContainer<A> { }

public record A(AsyncOwned<B> B);
public record B : IRequiresAsyncInitialization { public ValueTask InitializeAsync() => default; }
";
            _ = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out _);
            generatorDiagnostics.Verify(
                // (7,22): Error SI0103: Error while resolving dependencies for 'A': 'B' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(7, 22));
        }

        [Fact]
        public void AsyncFuncIsRequiredForOwnedOfAsyncInitializedDependency()
        {
            string userSource = @"
using System;
using System.Threading.Tasks;
using StrongInject;

[Register(typeof(A)), Register(typeof(B))]
public partial class Container : IContainer<A> { }

public record A(Func<Owned<B>> B);
public record B : IRequiresAsyncInitialization { public ValueTask InitializeAsync() => default; }
";
            _ = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out _);
            generatorDiagnostics.Verify(
                // (7,22): Error SI0103: Error while resolving dependencies for 'A': 'B' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(7, 22));
        }

        [Fact]
        public void AsyncFuncIsRequiredForAsyncOwnedOfAsyncInitializedDependency()
        {
            string userSource = @"
using System;
using System.Threading.Tasks;
using StrongInject;

[Register(typeof(A)), Register(typeof(B))]
public partial class Container : IContainer<A> { }

public record A(Func<AsyncOwned<B>> B);
public record B : IRequiresAsyncInitialization { public ValueTask InitializeAsync() => default; }
";
            _ = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out _);
            generatorDiagnostics.Verify(
                // (7,22): Error SI0103: Error while resolving dependencies for 'A': 'B' can only be resolved asynchronously.
                // Container
                new DiagnosticResult("SI0103", @"Container", DiagnosticSeverity.Error).WithLocation(7, 22));
        }

        [Fact]
        public void IOwnedCanBeInjected()
        {
            string userSource = @"
using System;
using StrongInject;

[Register(typeof(A)), Register(typeof(B))]
public partial class Container : IContainer<A> { }

public record A(Func<IOwned<B>> B);
public record B : IDisposable { public void Dispose() { } };
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::StrongInject.IOwned<global::B>> func_0_1;
        global::A a_0_0;
        func_0_1 = null;
        func_0_1 = () =>
        {
            global::StrongInject.Owned<global::B> owned_1_1;
            global::StrongInject.IOwned<global::B> iOwned_1_0;
            global::StrongInject.Owned<global::B> CreateOwnedB_2()
            {
                global::B b_1_0;
                b_1_0 = new global::B();
                return new global::StrongInject.Owned<global::B>(b_1_0, () =>
                {
                    ((global::System.IDisposable)b_1_0).Dispose();
                });
            }

            owned_1_1 = CreateOwnedB_2();
            iOwned_1_0 = (global::StrongInject.IOwned<global::B>)owned_1_1;
            return iOwned_1_0;
        };
        a_0_0 = new global::A(B: func_0_1);
        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::StrongInject.IOwned<global::B>> func_0_1;
        global::A a_0_0;
        func_0_1 = null;
        func_0_1 = () =>
        {
            global::StrongInject.Owned<global::B> owned_1_1;
            global::StrongInject.IOwned<global::B> iOwned_1_0;
            global::StrongInject.Owned<global::B> CreateOwnedB_2()
            {
                global::B b_1_0;
                b_1_0 = new global::B();
                return new global::StrongInject.Owned<global::B>(b_1_0, () =>
                {
                    ((global::System.IDisposable)b_1_0).Dispose();
                });
            }

            owned_1_1 = CreateOwnedB_2();
            iOwned_1_0 = (global::StrongInject.IOwned<global::B>)owned_1_1;
            return iOwned_1_0;
        };
        a_0_0 = new global::A(B: func_0_1);
        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
        });
    }
}");
        }

        [Fact]
        public void IAsyncOwnedCanBeInjected()
        {
            string userSource = @"
using System;
using System.Threading.Tasks;
using StrongInject;

[Register(typeof(A)), Register(typeof(B))]
public partial class Container : IContainer<A> { }

public record A(Func<IAsyncOwned<B>> B);
public record B : IAsyncDisposable { public ValueTask DisposeAsync() => default; };
";
            var comp = RunGeneratorWithStrongInjectReference(userSource, out var generatorDiagnostics, out var generated);
            generatorDiagnostics.Verify();
            comp.GetDiagnostics().Verify();
            var file = Assert.Single(generated);
            file.Should().BeIgnoringLineEndings(@"#pragma warning disable CS1998
partial class Container
{
    private int _disposed = 0;
    private bool Disposed => _disposed != 0;
    public void Dispose()
    {
        var disposed = global::System.Threading.Interlocked.Exchange(ref this._disposed, 1);
        if (disposed != 0)
            return;
    }

    TResult global::StrongInject.IContainer<global::A>.Run<TResult, TParam>(global::System.Func<global::A, TParam, TResult> func, TParam param)
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::StrongInject.IAsyncOwned<global::B>> func_0_1;
        global::A a_0_0;
        func_0_1 = null;
        func_0_1 = () =>
        {
            global::StrongInject.AsyncOwned<global::B> asyncOwned_1_1;
            global::StrongInject.IAsyncOwned<global::B> iAsyncOwned_1_0;
            global::StrongInject.AsyncOwned<global::B> CreateAsyncOwnedB_2()
            {
                global::B b_1_0;
                b_1_0 = new global::B();
                return new global::StrongInject.AsyncOwned<global::B>(b_1_0, async () =>
                {
                    await ((global::System.IAsyncDisposable)b_1_0).DisposeAsync();
                });
            }

            asyncOwned_1_1 = CreateAsyncOwnedB_2();
            iAsyncOwned_1_0 = (global::StrongInject.IAsyncOwned<global::B>)asyncOwned_1_1;
            return iAsyncOwned_1_0;
        };
        a_0_0 = new global::A(B: func_0_1);
        TResult result;
        try
        {
            result = func(a_0_0, param);
        }
        finally
        {
        }

        return result;
    }

    global::StrongInject.Owned<global::A> global::StrongInject.IContainer<global::A>.Resolve()
    {
        if (Disposed)
            throw new global::System.ObjectDisposedException(nameof(Container));
        global::System.Func<global::StrongInject.IAsyncOwned<global::B>> func_0_1;
        global::A a_0_0;
        func_0_1 = null;
        func_0_1 = () =>
        {
            global::StrongInject.AsyncOwned<global::B> asyncOwned_1_1;
            global::StrongInject.IAsyncOwned<global::B> iAsyncOwned_1_0;
            global::StrongInject.AsyncOwned<global::B> CreateAsyncOwnedB_2()
            {
                global::B b_1_0;
                b_1_0 = new global::B();
                return new global::StrongInject.AsyncOwned<global::B>(b_1_0, async () =>
                {
                    await ((global::System.IAsyncDisposable)b_1_0).DisposeAsync();
                });
            }

            asyncOwned_1_1 = CreateAsyncOwnedB_2();
            iAsyncOwned_1_0 = (global::StrongInject.IAsyncOwned<global::B>)asyncOwned_1_1;
            return iAsyncOwned_1_0;
        };
        a_0_0 = new global::A(B: func_0_1);
        return new global::StrongInject.Owned<global::A>(a_0_0, () =>
        {
        });
    }
}");
        }
    }
}
