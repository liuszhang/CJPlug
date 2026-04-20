using CJ.Plug.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Contracts
{
    public interface IModuleService
    {
        /// <summary>
        /// Event that is triggered when the features have been initialized.
        /// </summary>
        event Action? Initialized;

        /// <summary>
        /// Returns all features.
        /// </summary>
        IEnumerable<IModule> GetModules();

        /// <summary>
        /// Initializes all features.
        /// </summary>
        Task InitializeFeaturesAsync(CancellationToken cancellationToken = default);
    }
}
