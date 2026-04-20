using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Contracts
{
    public interface IAppBarService
    {
        /// <summary>
        /// Invoked when the app bar items change.
        /// </summary>
        event Action AppBarItemsChanged;

        /// <summary>
        /// A collection of components to render in the app bar.
        /// </summary>
        IEnumerable<RenderFragment> AppBarItems { get; }

        /// <summary>
        /// Adds a component to the app bar.
        /// </summary>
        /// <typeparam name="T">The type of the component.</typeparam>
        void AddAppBarItem<T>() where T : IComponent;
    }
}
