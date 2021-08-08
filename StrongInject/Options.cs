using System;

namespace StrongInject
{
    /// <summary>
    /// Provides options to configure a registration
    /// </summary>
    [Flags]
    public enum Options : long
    {
        Default = 0,

        #region As Options (bits 0 - 23)

        /// <summary>
        /// Register this type as all interfaces it implements
        /// </summary>
        AsImplementedInterfaces = 1L << 0,

        /// <summary>
        /// Registers this type as of its base classes except for <see cref="object"/>
        /// </summary>
        AsBaseClasses = 1L << 1,

        /// <summary>
        /// If this is registered as an instance of <see cref="IFactory{T}"/> or <see cref="IAsyncFactory{T}"/>
        /// (either directly or as a result of using <see cref="AsImplementedInterfaces"/>)
        /// registers this as an instance of T as well.
        /// </summary>
        UseAsFactory = 1L << 2,

        /// <summary>
        /// Meant to be used in conjunction with <see cref="UseAsFactory"/>.
        /// If this is registered as <see cref="IFactory{T}"/>, then we will apply the same options as are used for this to T.
        /// This means that we are using <see cref="AsImplementedInterfaces"/>, and T implements an interface, we will register T as that interface.
        /// Similarly if T is an <see cref="IFactory{A}"/> we will register it as a factory of A, and so on recursively.
        /// </summary>
        ApplySameOptionsToFactoryTargets = 1L << 3,

        /// <summary>
        /// Equivalent to `<see cref="AsImplementedInterfaces"/> | <see cref="AsBaseClasses"/>`
        /// </summary>
        AsImplementedInterfacesAndBaseClasses = AsImplementedInterfaces + AsBaseClasses,

        /// <summary>
        /// Equivalent to `<see cref="AsImplementedInterfaces"/> | <see cref="UseAsFactory"/>`
        /// </summary>
        AsImplementedInterfacesAndUseAsFactory = AsImplementedInterfaces + UseAsFactory,

        /// <summary>
        /// Equivalent to `<see cref="AsImplementedInterfaces"/> | <see cref="AsBaseClasses"/> | <see cref="UseAsFactory"/>`
        /// </summary>
        AsEverythingPossible = AsImplementedInterfacesAndBaseClasses + UseAsFactory + ApplySameOptionsToFactoryTargets,

        #endregion

        #region FactoryTargetScope Options (bits 24 - 31)

        /// <summary>
        /// Meant to be used in conjunction with <see cref="UseAsFactory"/>.
        /// If this is registered as <see cref="IFactory{T}"/>, then <see cref="IFactory{T}.Create"/> will be called once per resolution.
        /// See <see cref="Scope.InstancePerResolution"/> for more details.
        /// </summary>
        FactoryTargetScopeShouldBeInstancePerResolution = Scope.InstancePerResolution << 24,

        /// <summary>
        /// Meant to be used in conjunction with <see cref="UseAsFactory"/>.
        /// If this is registered as <see cref="IFactory{T}"/>, then <see cref="IFactory{T}.Create"/> will be called for every dependency.
        /// See <see cref="Scope.InstancePerDependency"/> for more details.
        /// </summary>
        FactoryTargetScopeShouldBeInstancePerDependency = Scope.InstancePerDependency << 24,

        /// <summary>
        /// Meant to be used in conjunction with <see cref="UseAsFactory"/>.
        /// If this is registered as <see cref="IFactory{T}"/>, then <see cref="IFactory{T}.Create"/> will only ever be called once, and T will be a singleton.
        /// See <see cref="Scope.SingleInstance"/> for more details.
        /// </summary>
        FactoryTargetScopeShouldBeSingleInstance = Scope.SingleInstance << 24,

        #endregion

        #region Other Options (bits 32 - 63)

        /// <summary>
        /// Don't apply decorators to any instances resolved using this registration
        /// </summary>
        DoNotDecorate = 1L << 32

        #endregion
    }
}
