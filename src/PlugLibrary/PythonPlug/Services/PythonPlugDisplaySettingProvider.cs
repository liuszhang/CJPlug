using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;

namespace PythonPlug.Services
{
    public class PythonPlugDisplaySettingProvider : IPlugDisplaySettingsProvider
    {
        public PlugDisplaySettings GetSettings()
        {
            return new PlugDisplaySettings(
                DefaultPlugColors.Scripting,
                null, 
                "Python", 
                "_content/PythonPlug/PythonPlug.ico"
            );
        }
    }
}
