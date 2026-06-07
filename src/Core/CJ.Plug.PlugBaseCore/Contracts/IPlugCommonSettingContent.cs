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

        /// <summary>
        /// 返回此插头的默认子插头列表（即预置动作）。
        /// 子插头与母插头共享同一 Type/PlugTypeKey，区别在于变量已预填具体值。
        /// 系统初始化时自动创建，用于"仅执行动作"模式开箱即用。
        /// 返回 null 或空列表表示无默认子插头。
        /// </summary>
        Task<List<Plug.Models.Plug.Plug>?> GetDefaultChildPlugs()
        {
            return Task.FromResult<List<Plug.Models.Plug.Plug>?>(null);
        }
    }
    
}
