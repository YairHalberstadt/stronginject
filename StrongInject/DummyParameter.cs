using System.ComponentModel;

namespace StrongInject.Internal
{
    /// <summary>
    /// A class with no possible value other than null. Used to mark an optional parameter which should never be set.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DummyParameter
    {
        private DummyParameter() { }
    }
}

