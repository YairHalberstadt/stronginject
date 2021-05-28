using System;

namespace StrongInject.Plugins
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
        /// Registers this type as of its base classes except for object
        /// </summary>
        AsBaseClasses = 1L << 1,

        /// <summary>
        /// If this is registered as an instance of IFactory&lt;T&gt; or IAsyncFactory&lt;T&gt;
        /// (either directly or as a result of using AsImplementedInterfaces)
        /// registers this as an instance of T as well.
        /// </summary>
        UseAsFactory = 1L << 2,

        /// <summary>
        /// Meant to be used in conjunction with UseAsFactory.
        /// If this is registered as IFactory&lt;T&gt;, then we will apply the same options as are used for this to T.
        /// This means that we are using AsImplementedInterfaces, and T implements an interface, we will register T as that interface.
        /// Similarly if T is an IFactory{A} we will register it as a factory of A, and so on recursively.
        /// </summary>
        ApplySameOptionsToFactoryTargets = 1L << 3,

        /// <summary>
        /// Equivalent to `AsImplementedInterfaces | AsBaseClasses`
        /// </summary>
        AsImplementedInterfacesAndBaseClasses = AsImplementedInterfaces + AsBaseClasses,

        /// <summary>
        /// Equivalent to `AsImplementedInterfaces | UseAsFactory`
        /// </summary>
        AsImplementedInterfacesAndUseAsFactory = AsImplementedInterfaces + UseAsFactory,

        /// <summary>
        /// Equivalent to `AsImplementedInterfaces | AsBaseClasses | UseAsFactory`
        /// </summary>
        AsEverythingPossible = AsImplementedInterfacesAndBaseClasses + UseAsFactory + ApplySameOptionsToFactoryTargets,

        #endregion

        #region FactoryTargetScope Options (bits 24 - 31)

        /// <summary>
        /// Meant to be used in conjunction with UseAsFactory.
        /// If this is registered as IFactory&lt;T&gt;, then IFactory&lt;T&gt;.Create will be called once per resolution.
        /// See Scope.InstancePerResolution for more details.
        /// </summary>
        FactoryTargetScopeShouldBeInstancePerResolution = Scope.InstancePerResolution << 24,

        /// <summary>
        /// Meant to be used in conjunction with UseAsFactory.
        /// If this is registered as IFactory&lt;T&gt;, then IFactory&lt;T&gt;.Create will be called for every dependency.
        /// See Scope.InstancePerDependency for more details.
        /// </summary>
        FactoryTargetScopeShouldBeInstancePerDependency = Scope.InstancePerDependency << 24,

        /// <summary>
        /// Meant to be used in conjunction with UseAsFactory.
        /// If this is registered as IFactory&lt;T&gt;, then IFactory&lt;T&gt;.Create will only ever be called once, and T will be a singleton.
        /// See Scope.SingleInstance for more details.
        /// </summary>
        FactoryTargetScopeShouldBeSingleInstance = Scope.SingleInstance << 24,

        #endregion

        #region Other Options (bits 32 - 63)

        /// <summary>
        /// Dont apply decorators to any instances resolved using this registration.
        /// </summary>
        DoNotDecorate = 1L << 32,

        #endregion
    }
}
