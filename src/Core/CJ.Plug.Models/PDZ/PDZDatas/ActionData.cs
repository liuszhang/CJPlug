using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


    public class ActionData : BaseVariable
    {
        [JsonIgnore]
        public PlugDataZone? PlugDataZone { get; set; }
        public int PlugDataZoneId { get; set; }


        //标识该参数属于哪个插头
        public string? PlugDefinitionId { get; set; }
        
        //--------插头动作相关属性
        //序号，用于展示和执行顺序
        public int SN { get; set; } = -1;
        //插头动作的TagID，用于区分同一插头中相同动作的不同配置数据
        public string? ActionIdentityId { get; set; }
        //标识插头动作的类型，用于获取插头动作的配置界面和原始参数
        public string? ActionPlugType { get; set; }
        //标识插头动作的ID，用于获取插头动作的配置界面和原始参数
        public string? ActionPlugRootDefinitionId { get; set; }

        
    }


