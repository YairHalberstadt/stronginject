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
This means that we are using `AsImplementedInterfaces`, and `T` implements an interface, we will register T as that inteface.
Similiarly if T is an `IFactory<A>` we will register it as a factory of `A`, and so on recusively.

#### AsImplementedInterfacesAndBaseClasses

Equivalant to `AsImplementedInterfaces | AsBaseClasses`

#### AsImplementedInterfacesAndUseAsFactory

Equivalant to `AsImplementedInterfaces | UseAsFactory`

#### AsEverythingPossible

Equivalant to `AsImplementedInterfaces | AsBaseClasses | UseAsFactory`

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

Dont apply decorators to any instances resolved using this registration

## Combining options

Options can be combined using the `|` operator, e.g. `Options.UseAsFactory | Options.DoNotDecorate`.

You can avoid repeating `Options` each time by adding `using static StrongInject.Options;` to the file, and then the above could be written as `UseAsFactory | DoNotDecorate`.
