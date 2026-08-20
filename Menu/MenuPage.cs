using System.Collections.Generic;

namespace Maytrix.Menu.Menu
{
    internal sealed class MenuPage
    {
        public MenuPage(string id, string title, params MenuItem[] items)
        {
            Id = id;
            Title = title;
            Items = new List<MenuItem>(items);
        }

        public string Id { get; }
        public string Title { get; }
        public IReadOnlyList<MenuItem> Items { get; }
    }
}
