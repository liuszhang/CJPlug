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
        TextMapping,
        ScriptType,              //用来保存是何种脚本，如java/python/c#等，插头执行时根据该配置匹配相应的脚本

        //device plug
        DeviceIP,                //设备 IP 地址
        DevicePort,              //设备端口
        DeviceProtocol,          //通信协议 (HTTP/TCP/UDP/Modbus TCP/Modbus RTU/Serial)
        DeviceBrand,             //设备品牌
        DeviceModel,             //设备型号
        DeviceTimeout,           //连接超时 (ms)
        DeviceAuthType,          //认证方式 (None/BasicAuth/ApiKey/BearerToken)
        DeviceUsername,          //认证用户名
        DevicePassword,          //认证密码
        DeviceApiKey,            //API Key / Token
        DeviceActions            //设备动作列表 (JSON)



    }
}
