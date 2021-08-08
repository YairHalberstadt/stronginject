<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Best Registration](#best-registration)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Best Registration

You can register multiple registrations for a type `T` which allows you to resolve `T[]` and other collections of `T`. However, when resolving just one instance of `T`, StrongInject requires that there is a *best registration* for `T`, or it will error.

A *best registration* is defined as follows:

1. Any registration declared directly on a module/container is *better* than registrations declared on other modules and imported by the module/container.
2. If there is a single registration which is *better* than all other registrations it is considered the *best registration*. Else there is no *best registration*.

So for example, imagine there is a Container, and two modules, ModuleA and ModuleB.

If all three define a registration for SomeInterface, then the one defined on the container will always be the *best registration*.

If just ModuleA and ModuleB define a registration for SomeInterface then things will depend:

- If Container imports both modules, resolving SomeInterface will always result in an error (even if ModuleA imports ModuleB or vice versa).
- If Container imports ModuleA, and ModuleA imports ModuleB, then ModuleA's registration will be the *best registration*.
- If Container imports ModuleB, and ModuleB imports ModuleA, then ModuleB's registration will be the *best registration*.

To fix errors as a result of multiple registrations for a type, the simplest thing to do is to add a single *best registration* directly to the container. If the container already has multiple registrations for the type, then move those registrations to a separate module and import them.
