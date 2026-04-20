using CJ.Plug.Models.Plug;

namespace CJ.Plug.PlugBaseCore.Contracts
{
    /// <summary>
    /// Provides mappings between activity types and icons.
    /// </summary>
    public interface IPlugDisplaySettingsProvider
    {
        /// <summary>
        /// Returns a dictionary of activity type to display settings.
        /// </summary>
        /// <returns></returns>
        PlugDisplaySettings GetSettings();
    }
}