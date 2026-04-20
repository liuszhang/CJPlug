using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


    public class PDZVariable:BaseVariable
    {
        [JsonIgnore]
        public PlugDataZone? PlugDataZone { get; set; }
        public int? PlugDataZoneId { get; set; }


        //标识该参数属于哪个插头
        public string? PlugDefinitionId { get; set; }
        //是否将数据保存回插头
        public bool? SaveToPlug { get; set; } = false;
        //标识参数Tag,区分普通参数、动作数据、动作参数、流程图数据等
        public string? Tag { get; set; } = PDZVariableTagEnum.PDZVariable.ToString();

        //--------插头动作相关属性，统一整合至PDZVariable中，方便更新和管理
        //序号，用于展示和执行顺序
        public int SN { get; set; } = -1;
        //插头动作的TagID，用于区分同一插头中相同动作的不同配置数据
        public string? ActionIdentityId { get; set; }
        //标识插头动作的类型，用于获取插头动作的配置界面和原始参数
        public string? PlugActionType { get; set; }
        //标识插头动作的ID，用于获取插头动作的配置界面和原始参数
        public string? PlugActionDefinitionId { get; set; }

        
    }


