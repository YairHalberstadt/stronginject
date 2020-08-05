using System;
using System.Collections.Generic;
using System.Text;

namespace StrongInject
{
    public class StrongInjectException : Exception
    {
        public StrongInjectException(string message) : base(message)
        {
        }
    }
}
