using CJ.Plug.Models.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Services
{
    public class DefaultModuleService : IModuleService
    {
        private readonly IEnumerable<CJCore.Framework.Abstractions.IModule> _features;

        public DefaultModuleService(IEnumerable<CJCore.Framework.Abstractions.IModule> features)
        {
            _features = features;
        }

        public event Action? Initialized;

        public IEnumerable<CJCore.Framework.Abstractions.IModule> GetModules()
        {
            return _features.ToList();
        }

        public async Task InitializeFeaturesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var feature in GetModules())
            {
                await feature.InitializeAsync(cancellationToken);
            }

            OnInitialized();
        }

        private void OnInitialized()
        {
            Initialized?.Invoke();
        }
    }
}
