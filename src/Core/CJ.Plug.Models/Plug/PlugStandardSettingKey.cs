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
        SupportRemoteView,   //是否支持远程查看，主要针对桌面类插头，开启后可以在图站远程查看执行过程中的界面和操作，类似远程桌面分享的功能


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
