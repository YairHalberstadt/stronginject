<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Design Decisions](#design-decisions)
  - [Why attributes?](#why-attributes)
  - [Why Async](#why-async)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Design Decisions

This document intends to explain my thought process when designing StrongInject's API.

It's intended so that if anybody ever asks me why StrongInject works a certain way, I can point them to this document.

## Why attributes?

Many DI frameworks allow you to use fluent syntax to register your services. Something like `builder.Register<MyService>().As<IService>().SingleInstance()`.

I love these fluent APIs, so why didn't I use them for StrongInject?

Now obviously FluentLang is compile time only, so it wouldn't be able to run these builders. But we could have a convention for where to put your registration code, and StrongInject would parse and iterpret this code at compile time to calculate what you've registered.

Whilst this could have worked, there were a number of reasons not to do this:

1. **Complexity:** It's much easier to read attributes at compile time than code.
2. **We need attributes anyway:** If you define a module in one assembly, and then reference it in a another, Stronginject can't see the code you wrote in the module. So we would have to have a source generator running which converted the module fluent syntax to a bunch of attributes on the module, and then have another source generator read those attributes to work out the registrations. Since we definitely need to parse and understand these attributes, adding a fluent API is just doubling the public surface area *and* the amount of work I need to do.
3. **A fluent Api would be a pit of failure:** The best thing about fluent APIs is they're just code. I can write my own extension methods, or only register something if some condition is true, or whatever. However since StrongInject never actually runs the builder code, none of this stuff would work automatically. A subset of C# might be supported at great effort by attempting to interpret it at compile time, but users would never be able to predict whether a given piece of code would work. I know all the things that my users could possibly do, and I can make sure that all of them either work, or have a suitable error message.

All this suggests that attempting to support a fluent API would risk spiralling complexity, and would actually probably lead to a worse user experience (honestly, attributes are not that bad :-). Give them a try!).

Furthermore, if roslyn ever provides the capability to [run one source generator on the results of another](https://github.com/dotnet/roslyn/discussions/48358), it would be perfectly within reason for someone to write their own frontend to StrongInject that reads code and converts them to attributes, which StrongInject then reads and generates a container.

## Why Async

I don't know of any other .Net IOC container that supports async resolution.

Usually you get some sort of explanation like [this](https://github.com/autofac/Autofac/issues/751#issuecomment-221132638).

> Without going too deep, I'm not sure async resolution of anything is something we'd really jump into since resolution (effectively object creation) should be really, really slim - so slim you'd never do it asynchronously; or optimized such that expensive things are done as little as possible (like registering something as a singleton) so, again, you'd never do it asynchronously.

I'm afraid I disagree, and I'd even risk calling it a bit of a cop out.

It's super super common to need to load data from a database, or load a config from a file, as part of resolution. The lack of support for this in containers tends to lead to code either resolution code becoming super complicated, where you have to resolve all the async stuff inside your bootstrapper, initialize them, and then resolve the services you actually need normally, or you have to make all code paths in your project async, so that the very first time they run they can initialize your services.

IMO the lack of async support causes far more problems than it potentially prevents happening. Maybe some people disagree, but *every single .NET IOC container author*?

I think the real reason is that's it's super difficult for a traditional IOC container.

1. Most IOC container generate IL at runtime. `async/await` is a C# feature, so they're stuck with using the much less efficient `Task.ContinueWith` apis, or reimplementing the C# compiler and generating a state machine by hand. Neither option is very appealing.
2. Most of the time you don't require an async resolution, so you need 2 APIs - `Resolve` and `ResolveAsync`. Calling the non-async version when async resolution is required would have to result in a runtime exception, which isn't a very friendly API.

StrongInject has neither issue since it generates C#, and can provide compile time errors if you implement `IContainer<T>` instead of `IAsyncContainer<T>` or resolve a `Func<T>` instead of a `Func<Task<T>>`.

The next reason given in the above lined discussion is:

> Executing async code during object instantiation isn't really a good idea, even if it's a factory doing that execution. At some point it has to become synchronous since things like singletons need to be originally initialized in a synchronous fashion, locks need to be made so things don't get double-instantiated, and so on.

This doesn't really make a lot of sense to me. Firstly not everything that needs to do async work is going to be a singleton. Secondly StrongInject makes sure that singletons are only instantiated once, and uses a SemaphoreSlim to allow `awaiting` inside a lock. We can then parallelize any async resolutions to hugely improve resolution performance.

As such making this an async container was the obvious decision.
