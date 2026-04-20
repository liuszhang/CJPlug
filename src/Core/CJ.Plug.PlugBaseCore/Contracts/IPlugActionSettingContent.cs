using CJ.Plug.Models.PlugAction;
using Microsoft.AspNetCore.Components;
using System.Text.Json.Nodes;

namespace CJ.Plug.PlugBaseCore.Contracts
{
    public interface IPlugActionSettingContent
    {
        /// <summary>
        /// 当插头作为动作时，获取动作设置内容的渲染片段
        /// </summary>
        /// <param name="ActionItem"></param>
        /// <returns></returns>
        Task<RenderFragment?> GetPlugActionSettingContent(Plug.Models.Plug.Plug ActionItem);
    }
    
}
