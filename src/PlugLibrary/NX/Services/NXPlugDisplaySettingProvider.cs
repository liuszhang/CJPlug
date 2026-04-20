
using CJ.Plug_Aspire.Models.Contracts;
using CJ.Plug_Aspire.Models.Plug;

namespace NX.Services
{
    public class NXPlugDisplaySettingProvider : IPlugDisplaySettingsProvider
    {
        public PlugDisplaySettings GetSettings()
        {
            return new PlugDisplaySettings(DefaultPlugColors.Timer,null, "NX" ,"_content/NX/img/icons/nx.ico");
        }
    }
}
