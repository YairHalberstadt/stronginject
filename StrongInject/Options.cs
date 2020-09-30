using System;

namespace StrongInject
{
    [Flags]
    public enum Options : long
    {
        Default = 0,
        AsImplementedInterfaces = 1L << 0,
        AsBaseClasses = 1L << 1,
        UseAsFactory = 1L << 2,
        ApplySameOptionsToFactoryTargets = 1L << 3,

        AsImplementedInterfacesAndBaseClasses = AsImplementedInterfaces + AsBaseClasses,
        AsImplementedInterfacesAndUseAsFactory = AsImplementedInterfaces + UseAsFactory,
        AsEverythingPossible = AsImplementedInterfacesAndBaseClasses + UseAsFactory + ApplySameOptionsToFactoryTargets,

        FactoryTargetScopeShouldBeInstancePerResolution = Scope.InstancePerResolution << 24,
        FactoryTargetScopeShouldBeInstancePerDependency = Scope.InstancePerDependency << 24,
        FactoryTargetScopeShouldBeSingleInstance = Scope.SingleInstance << 24,

        DoNotDecorate = 1L << 32
    }
}
