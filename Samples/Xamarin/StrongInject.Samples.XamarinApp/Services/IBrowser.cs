using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StrongInject.Samples.XamarinApp.Services
{
    public interface IBrowser
    {
        Task OpenAsync(string uri);
    }
}
