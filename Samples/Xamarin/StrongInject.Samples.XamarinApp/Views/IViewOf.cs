using StrongInject.Samples.XamarinApp.ViewModels;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp.Views
{
    /// <summary>
    /// Should only be implemented by a page.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IViewOf<T> : ILayout, IPageController, IVisualElementController, IElementController, IElementConfiguration<Page> where T : BaseViewModel
    {

    }
}
