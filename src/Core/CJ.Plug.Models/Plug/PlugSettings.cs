using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Plug
{
    public class PlugSettings
    {
        public PlugSettings()
        {
        }
        public PlugSettings(string? plugId)
        {
            PlugId = plugId;
        }

        public string? PlugId { get; set; }
        public string? PlugDisplayName { get; set; }  //设置初始化时的插头显示名称
        public string? PlugTypeKey { get; set; }
        //public string? PlugType { get; set; }
        public string? PlugCategory { get; set; }   //插头类别，决定了执行时走哪个执行器，比如桌面类、接口类、脚本类等

        //插头的初始化参数
        public List<BaseVariable>? InitVariables { get; set; } = new();


        //用于序列化保存一些不固定的设置项，比如 Outcomes、Timeout 等等，具体内容由各插件自行定义和使用，非必要不使用，尽量使用插头参数管理
        public Dictionary<string, string> Settings { get; set; } = new Dictionary<string, string>();

        public string? GetSetting(string key)
        {
            if (Settings.ContainsKey(key))
            {
                return Settings[key];
            }
            return null;
        }

        public void SetSetting(string key, string value)
        {
            if (Settings.ContainsKey(key))
            {
                Settings[key] = value;
            }
            else
            {
                Settings.Add(key, value);
            }
        }

        public string? GetSettingsJson()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }


    }
}
