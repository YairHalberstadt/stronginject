namespace StrongInject.Plugins
{
    public enum Scope
    {
        /// <summary>
        /// Default scope.
        /// A single instance is shared between all dependencies created for a single resolution.
        /// For example if 'A' debends on 'B' and 'C', and 'B' and 'C' both depend on an instance of 'D',
        /// then when 'A' is resolved 'B' and 'C' will share the same instance of 'D'.
        /// 
        /// Note every SingleInstance dependency defines a seperate resolution, 
        /// so if 'B' and/or 'C' are SingleInstance they would not share an instance of 'D'.
        /// 
        /// Similiarly every lambda defines a seperate resolution, so if A depends on Func&lt;B&gt;,
        /// then each time Func&lt;B&gt; is invoked a fresh instance of both B and D will be created.
        /// </summary>
        InstancePerResolution = 0,

        /// <summary>
        /// A new instance is created for every usage.
        /// For example even if type 'B' appears twice in the constructor of 'A',
        /// two different instances will be passed into the constructor.
        /// </summary>
        InstancePerDependency = 1,

        /// <summary>
        /// A single instance will be shared across all dependencies, from any resolution
        /// </summary>
        SingleInstance = 2,
    }
}