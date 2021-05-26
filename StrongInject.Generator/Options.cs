using System;

namespace StrongInject.Generator
{
    /// <summary>
    /// Provides options to configure a registration
    /// </summary>
    [Flags]
    public enum Options : long
    {
        Default = 0,

        #region As Options (bits 0 - 23)
        
        AsImplementedInterfaces = 1L << 0,
        
        AsBaseClasses = 1L << 1,
        
        UseAsFactory = 1L << 2,
        
        ApplySameOptionsToFactoryTargets = 1L << 3,

        #endregion

        #region FactoryTargetScope Options (bits 24 - 31)
        
        FactoryTargetScopeShouldBeInstancePerResolution = Scope.InstancePerResolution << 24,
        
        FactoryTargetScopeShouldBeInstancePerDependency = Scope.InstancePerDependency << 24,
        
        FactoryTargetScopeShouldBeSingleInstance = Scope.SingleInstance << 24,

        #endregion

        #region Other Options (bits 32 - 63)
        
        DoNotDecorate = 1L << 32

        #endregion
    }
}
