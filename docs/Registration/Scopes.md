<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Scopes](#scopes)
  - [List Of Scopes](#list-of-scopes)
    - [InstancePerResolution](#instanceperresolution)
    - [InstancePerDependency](#instanceperdependency)
    - [SingleInstance](#singleinstance)
  - [Disposal](#disposal)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Scopes

All registrations have a particular Scope. This tells StrongInject how long an instance resolved using that registration should live for, and how widely it should be shared.

## List Of Scopes

There are currently 3 possible scopes. The default scope is `InstancePerResolution` but the scope can be modified per registration via the `Scope` enum.

### InstancePerResolution

The default scope.

A single instance is shared between all dependencies created for a single resolution.

For example if `A` debends on `B` and `C`, and `B` and `C` both depend on an instance of `D`, then when `A` is resolved `B` and `C` will share the same instance of `D`.

Note every SingleInstance dependency defines a seperate resolution, so if `B` and/or `C` are `SingleInstance` they would not share an instance of `D`. Similiarly every lambda defines a seperate resolution, so if `A` depends on `Func<B>`, then each time `Func<B>` is invoked a fresh instance of both `B` and `D` will be created.

### InstancePerDependency

A new instance is created for every usage.

For example even if type `B` appears twice in the constructor of `A`, two different instances will be passed into the constructor.

### SingleInstance

A single instance will be shared across all dependencies, from any resolution.

## Disposal

A `SingleInstance` instance will be disposed when the container is disposed (except [`Instance`](https://github.com/YairHalberstadt/stronginject/wiki/InstanceRegistration) fields and properties which are never disposed).

An `InstancePerResolution` or `InstancePerDependency` instance will be disposed when StrongInject knows it can no longer be used. This means:

- If it is a dependency of a `SingleInstance` it will be disposed when the container is disposed.

- Else, if it was created as part of a call to `Run`/`RunAsync`, it will be disposed, once the delegate paramater to `Run`/`RunAsync` completes.

- Else, if it was created as part of a call to `Resolve`/`ResolveAsync`, it will be disposed once the returned `IOwned`/`IOwnedAsync` is disposed.

