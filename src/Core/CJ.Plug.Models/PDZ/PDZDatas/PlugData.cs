using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


    public class PlugData:BaseVariable
    {
        [JsonIgnore]
        public PlugDataZone? PlugDataZone { get; set; }
        public int PlugDataZoneId { get; set; }


        public string? PlugDefinitionId { get; set; }  //插头ID
        /// <summary>插头唯一标识符，用于匹配自定义配置界面和执行方法。内置插头如"CMDPlug"/"PythonPlug"，手动创建可为空。</summary>
        public string? PlugTypeKey { get; set; }
        public bool OnlyExecuteAction { get; set; } = false; //执行时是否只执行动作而忽略插头本身执行

        public string? ParentPlugDefinitionId { get; set; } //父插头ID,用于插头嵌套

        public string? Creater { get; set; } //创建者
        public string? WorkPath { get; set; } //工作目录，用于存放和访问实体文件

        public string? PlugPosition { get; set; } //插头位置，格式为"X Y"，用于可视化编辑器
    }


