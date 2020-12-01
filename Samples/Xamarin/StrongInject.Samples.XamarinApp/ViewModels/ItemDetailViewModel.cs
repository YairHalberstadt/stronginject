using StrongInject.Samples.XamarinApp.Models;
using Xamarin.Forms;

namespace StrongInject.Samples.XamarinApp.ViewModels
{
    [QueryProperty(nameof(ItemId), nameof(ItemId))]
    public class ItemDetailViewModel : BaseViewModel
    {
        public ItemDetailViewModel(Item item)
        {
            Id = _itemId = item.Id;
            Text = item.Text;
            Description = item.Description;
        }

        private readonly string _itemId = null!;
        private string _text = null!;
        private string _description = null!;

        public string Id { get; set; } = null!;

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string ItemId
        {
            get
            {
                return _itemId;
            }
        }
    }
}
