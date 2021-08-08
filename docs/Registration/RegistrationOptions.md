<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
## Table Of Contents

- [Registration Options](#registration-options)
  - [Members](#members)
    - [As Options](#as-options)
      - [AsImplementedInterfaces](#asimplementedinterfaces)
      - [AsBaseClasses](#asbaseclasses)
      - [UseAsFactory](#useasfactory)
      - [ApplySameOptionsToFactoryTargets](#applysameoptionstofactorytargets)
      - [AsImplementedInterfacesAndBaseClasses](#asimplementedinterfacesandbaseclasses)
      - [AsImplementedInterfacesAndUseAsFactory](#asimplementedinterfacesanduseasfactory)
      - [AsEverythingPossible](#aseverythingpossible)
    - [Factory Target Scope Options](#factory-target-scope-options)
      - [FactoryTargetScopeShouldBeInstancePerResolution](#factorytargetscopeshouldbeinstanceperresolution)
      - [FactoryTargetScopeShouldBeInstancePerDependency](#factorytargetscopeshouldbeinstanceperdependency)
      - [FactoryTargetScopeShouldBeSingleInstance](#factorytargetscopeshouldbesingleinstance)
    - [Other Options](#other-options)
      - [DoNotDecorate](#donotdecorate)
  - [Combining options](#combining-options)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

# Registration Options

The `Options` enum is a [flags](https://docs.microsoft.com/en-us/dotnet/api/system.flagsattribute) enum, which can be used to modify certain registrations.

## Members

The enum provides the following members:

### As Options

These members configure what a registration should be registered as.

#### AsImplementedInterfaces

Register this type as all interfaces it implements.

#### AsBaseClasses

Registers this type as of its base classes except for `object

#### UseAsFactory

If this is registered as an instance of `IFactory<T>` or `IAsyncFactory<T>` either directly or as a result of using `AsImplementedInterfaces`) registers this as an instance of `T` as well.

#### ApplySameOptionsToFactoryTargets

Meant to be used in conjunction with `UseAsFactory`.
If this is registered as `IFactory<T>`, then we will apply the same options as are used for this to T.
This means that we are using `AsImplementedInterfaces`, and `T` implements an interface, we will register T as that interface.
Similarly if T is an `IFactory<A>` we will register it as a factory of `A`, and so on recursively.

#### AsImplementedInterfacesAndBaseClasses

Equivalent to `AsImplementedInterfaces | AsBaseClasses`

#### AsImplementedInterfacesAndUseAsFactory

Equivalent to `AsImplementedInterfaces | UseAsFactory`

#### AsEverythingPossible

Equivalent to `AsImplementedInterfaces | AsBaseClasses | UseAsFactory`

### Factory Target Scope Options

These modify the scope of `T` for an `IFactory<T>`, and are meant to be used in conjunction with `UseAsFactory`.

#### FactoryTargetScopeShouldBeInstancePerResolution

If this is registered as `IFactory<T>`, then `IFactory<T>.Create` will be called once per resolution.

#### FactoryTargetScopeShouldBeInstancePerDependency

If this is registered as `IFactory<T>`, then `IFactory<T>.Create` will be called for every dependency.

#### FactoryTargetScopeShouldBeSingleInstance

If this is registered as `IFactory<T>`, then `IFactory<T>.Create` will only ever be called once, and T will be a singleton.

### Other Options

#### DoNotDecorate

Don't apply decorators to any instances resolved using this registration

## Combining options

Options can be combined using the `|` operator, e.g. `Options.UseAsFactory | Options.DoNotDecorate`.

You can avoid repeating `Options` each time by adding `using static StrongInject.Options;` to the file, and then the above could be written as `UseAsFactory | DoNotDecorate`.
