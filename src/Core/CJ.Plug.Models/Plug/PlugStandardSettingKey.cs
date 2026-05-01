using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Plug
{
    /// <summary>
    /// 插头的一些标准设置键
    /// </summary>
    public enum PlugSettingKey
    {
        //share
        InitVariables,
        InitVariableTypes,
        Outcomes,
        Category,  //插头主分类，桌面类/Web类/脚本类
        Group,
        IsContainerPlug,
        IconData,

        //exe plug
        ToolPath,
        ToolName,
        ToolVersion,
        ToolDisplayName,
        CommandLineShema,
        DownloadFromServerWhenRun,
        FileId,


        //web plug
        Url,
        BrowserType,
        Method,
        Authorization,
        DisableAuthorizationHeaderValidation,
        ContentType,
        Content,

        //script plug
        TextMapping



    }
}
