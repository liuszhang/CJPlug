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
        public string? PlugType { get; set; } //设置初始化时的插头类型
        public string? PlugDisplayName { get; set; }  //设置初始化时的插头显示名称
        public string? PlugTypeKey { get; set; }
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

        public void RemoveSetting(string key)
        {
            if (Settings.ContainsKey(key))
            {
                Settings.Remove(key);
            }
        }

        public void ClearSettings()
        {
            Settings.Clear();
        }

        public void SetSettings(Dictionary<string, string> settings)
        {
            Settings = settings;
        }

        public Dictionary<string, string> GetSettings()
        {
            return Settings;
        }

        public string? GetSettingsJson()
        {
            return System.Text.Json.JsonSerializer.Serialize(this);
        }

        public void SetSettingsJson(string json)
        {
            Settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        }



    }
}
