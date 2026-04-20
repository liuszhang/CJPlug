using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Relation
{
    public class CommonRelation
    {
        public int? Id { get; set; }

        //关系表类型，如用户-市场插头，插头-插头动作，目的是将多个关系表融合一个表
        public string? RelationCategory { get; set; }

        public int? RoleAId { get; set; }
        public int? RoleBId { get; set; }
        public string? RoleAName { get; set; }
        public string? RoleBName { get; set; }
        //关系类型，如发布者，使用者
        public string? RelationType { get; set; }
        //保存关系相关配置信息，比如：工具和图站绑定后的特殊配置信息，使用时序列化为字典表
        public string? RelationSetting { get; set; } = "";

        public void SetRelationSetting(string key, string value)
        {
            var settings = string.IsNullOrEmpty(RelationSetting) ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>?>(RelationSetting);
            settings[key] = value;
            this.RelationSetting=JsonSerializer.Serialize(settings);
        }

        public string? GetRelationSetting(string key)
        {
            var settings = JsonSerializer.Deserialize<Dictionary<string, string>?>(RelationSetting) ?? new Dictionary<string, string>();
            return settings[key];
        }

        public Dictionary<string, string> GetRelationSettingsDic()
        {
            var settings = JsonSerializer.Deserialize<Dictionary<string, string>?>(RelationSetting) ?? new Dictionary<string, string>();
            return settings;
        }

        public void SetRelationSettingDic(Dictionary<string, string> settings)
        {
            this.RelationSetting = JsonSerializer.Serialize(settings);
        }
    }
}
