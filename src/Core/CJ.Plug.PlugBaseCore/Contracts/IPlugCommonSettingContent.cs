using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.AspNetCore.Components;

namespace CJ.Plug.PlugBaseCore.Contracts
{
    public interface IPlugCommonSettingContent
    {
        //插头实现配置内容页
        //Task<RenderFragment?> GetPlugCommonSettingContent(string PlugTypeKey);
        //Task<RenderFragment?> GetPlugCommonSettingContent(CJ.Plug.Models.Plug.Plug Plug);
        Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context);

        //插头实现基础插头配置信息
        Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings();            
            return Task.FromResult<PlugSettings?>(settings);
        }
    }
    
}
