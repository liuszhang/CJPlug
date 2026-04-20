using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace CJ.Plug.Models.Services
{
    public class DefaultAppBarService : IAppBarService
    {
        private readonly ICollection<RenderFragment> _appBarItems = new List<RenderFragment>();
        private int _appBarSequence;

        public event Action? AppBarItemsChanged;
        public IEnumerable<RenderFragment> AppBarItems => _appBarItems.ToList();

        public void AddAppBarItem<T>() where T : IComponent
        {
            _appBarItems.Add(builder => builder.CreateComponent<T>(ref _appBarSequence));
            AppBarItemsChanged?.Invoke();
        }
    }
}
