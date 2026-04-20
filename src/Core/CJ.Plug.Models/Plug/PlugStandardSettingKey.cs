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
        Category,
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
