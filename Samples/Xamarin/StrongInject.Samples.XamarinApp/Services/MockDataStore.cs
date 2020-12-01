using StrongInject.Samples.XamarinApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StrongInject.Samples.XamarinApp.Services
{
    public class MockDataStore : IDataStore<Item>
    {
        readonly List<Item> _items;

        public MockDataStore()
        {
            _items = new List<Item>()
            {
                new Item(id: Guid.NewGuid().ToString(), text: "First item", description: "This is an item description." ),
                new Item(id: Guid.NewGuid().ToString(), text: "Second item", description: "This is an item description." ),
                new Item(id: Guid.NewGuid().ToString(), text: "Third item", description: "This is an item description." ),
                new Item(id: Guid.NewGuid().ToString(), text: "Fourth item", description: "This is an item description." ),
                new Item(id: Guid.NewGuid().ToString(), text: "Fifth item", description: "This is an item description." ),
                new Item(id: Guid.NewGuid().ToString(), text: "Sixth item", description: "This is an item description." ),
            };
        }

        public async Task<bool> AddItemAsync(Item item)
        {
            _items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> UpdateItemAsync(Item item)
        {
            var oldItem = _items.Where((Item arg) => arg.Id == item.Id).FirstOrDefault();
            _items.Remove(oldItem);
            _items.Add(item);

            return await Task.FromResult(true);
        }

        public async Task<bool> DeleteItemAsync(string id)
        {
            var oldItem = _items.Where((Item arg) => arg.Id == id).FirstOrDefault();
            _items.Remove(oldItem);

            return await Task.FromResult(true);
        }

        public async Task<Item> GetItemAsync(string id)
        {
            return await Task.FromResult(_items.FirstOrDefault(s => s.Id == id));
        }

        public async Task<IEnumerable<Item>> GetItemsAsync(bool forceRefresh = false)
        {
            return await Task.FromResult(_items);
        }
    }
}