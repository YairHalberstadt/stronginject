using System;
using System.Collections.Generic;
using System.Text;

namespace StrongInject
{
    public sealed class StrongInjectException : Exception
    {
        public StrongInjectException(string message) : base(message)
        {
        }
    }
}
