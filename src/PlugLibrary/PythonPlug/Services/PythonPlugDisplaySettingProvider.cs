

using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;

namespace PythonPlug.Services
{
    public class PythonPlugDisplaySettingProvider : IPlugDisplaySettingsProvider
    {
        public PlugDisplaySettings GetSettings()
        {
            return new PlugDisplaySettings(DefaultPlugColors.Timer,null, "NX" ,"_content/NX/img/icons/nx.ico");
        }
    }
}
