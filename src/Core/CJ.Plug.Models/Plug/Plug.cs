using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Station;
using CJ.Plug.Models.VariableType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Plug
{
    public class Plug
    {
        public int? Id { get; set; }
        public string? DefinitionId { get; set; }
        public string? ProcessId { get; set; }
        //父插头的定义ID，父插头可能是流程、容器类插头等
        public string? ParentPlugDefinitionId { get; set; }
        public string? Description { get; set; }
        public string? Name { get; set; }
        //插头核心活动类型，用于引擎进行活动执行
        //public string? CoreType { get; set; } = "CJ.CommonCorePlugActivity";
        /// <summary>插头唯一标识符，用于匹配自定义配置界面和执行方法。内置插头如"CMDPlug"/"PythonPlug"，手动创建可为空。</summary>
        public string? PlugTypeKey { get; set; }
        public string? Category { get; set; } = ToolTypeEnum.桌面类_自研.ToString(); //记录插头的种类，如桌面类，接口类，脚本类等
        public string? GroupName { get; set; }   //记录插头在插头库中的分组名称，对应引擎中的Category
        public string? RealValuePath { get; set; } //如果有文件操作，保存文件在文件服务器的真实路径，便于后续数据处理，也是为了简化从PlugSetting中取值的操作
        public string? WorkPath { get; set; }  //插头的工作目录（相对路径）
        public string? Status { get; set; }
        //public bool IsPlugIn { get; set; } = false;
        //public bool IsPlugIning { get; set; } = false;
        //执行时是否只执行动作而忽略插头本身执行
        public bool OnlyExecuteAction { get; set; } = false;
        public bool SupportNoWindow { get; set; } = true;
        public bool NeedToolEnvirment { get; set; } = true;
        //当作为流程执行时的触发活动ID,已经转移到PDZ中，弃用
        [Obsolete]
        public string? TriggerPlugDefinitionId { get; set; }
        [Obsolete]
        public string? TriggerPlugId { get; set; }
        public string? Creater { get; set; }

        //用于在插头管理中区分仅动作或插头，以及用于拖拽区域标识
        public string? Tag { get; set; } = "Plug";

        //插头图标（支持 Material Icon 名称或自定义图片路径）
        public string? Icon { get; set; }

        //保存插头的配置信息，由PlugSetting序列化得到，使用时反序列化为PlugSettings类
        public string? PlugSettingsJson { get; set; }



        //是否为原始插头，为false则为插头实例
        public bool IsRootPlug { get; set; }=false;
        //是否为用户自定义插头
        //public bool IsCustomeToolPlug {  get; set; } =false;
        //是否为系统通过依赖注入的自带插头,此类插头不允许删除
        //public bool IsSystemInitPlug { get; set; } = false;
        //是否为通过流程复用的插头
        //public bool IsProcessToPlug { get; set; } = false;
        //是否为容器类插头
        public bool IsContainerPlug { get; set; } = false;

        //标记插头的创建方式
        public string? CreateType { get; set; } = PlugCreateTypeEnum.None.ToString(); //SystemInit,User,Process,Root,System,etc...


        //是否将TAS插头展示在组件库
        public bool ShowInPlugLibrary { get; set; } = false;

        //拖拽排序序号，用于持久化插头在同组内的显示顺序
        public int? SortOrder { get; set; }


        //流程引擎相关数据
        public string? ActivityNodeId { get; set; }
        public string? ActivityVersion { get; set; } = "1";
        //打通Plug和Activity的JSON数据
        public string? ActivityJsonData { get; set; }
        public string? ActivityMetaData { get; set; }

        //保存自定义插头设计界面的Json数据
        public string? GuiJsonData { get; set; }

        //public int? MarketPlugId { get; set; }


        public int? ToolId { get; set; } //工具ID（唯一权威来源，通过此 ID 查找 Tool 实体获取所有工具信息）

        [Obsolete("Use ToolId to look up Tool entity instead")]
        public string? ToolName { get; set; }

        [Obsolete("Use ToolId to look up Tool entity instead")]
        public string? ToolDisplayName { get; set; }

        [Obsolete("Use ToolId to look up Tool entity instead")]
        public string? ToolCommandLineShema { get; set; }

        [Obsolete("Use ToolId to look up Tool entity instead")]
        public string? ToolVersion { get; set; }

        [Obsolete("Use ToolId to manage tool paths instead")]
        public string? ToolVersionPath { get; set; }
        public List<string>? ToolVersions { get; set; } = new(); //工具版本列表，暂时不用

        //public List<PlugAction.PlugAction>? PlugActions { get; set; }= new List<PlugAction.PlugAction>();
        //用于储存插头本身的参数，使用时可能会作为PDZ中插头参数创建的依据
        public List<PlugVariable>? PlugVariables { get; set; } = new List<PlugVariable>();

        /// <summary>
        /// 将插头转为流程活动的方法
        /// </summary>
        /// <returns></returns>
        public JsonObject ToActivityJson()
        {
            if (!string.IsNullOrEmpty(ActivityJsonData))
            {
                // 方法1：动态反序列化
                JsonObject rootDynamic = JsonNode.Parse(ActivityJsonData).AsObject();

                return rootDynamic;
            }
            return new JsonObject();
        }

        /// <summary>
        /// 获取流程图数据（从插头自身的 ActivityJsonData）
        /// </summary>
        public JsonObject? GetFlowchartJson()
        {
            if (string.IsNullOrEmpty(ActivityJsonData))
                return null;
            try
            {
                return JsonNode.Parse(ActivityJsonData)?.AsObject();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 保存流程图数据到插头的 ActivityJsonData
        /// </summary>
        public void SetFlowchartJson(JsonObject flowchart)
        {
            ActivityJsonData = flowchart?.ToJsonString();
        }

        /// <summary>
        /// 序列化方式复制实例
        /// </summary>
        /// <returns></returns>
        public Plug DeepCopy()
        {
            var newPlug = new Plug();
            var tmpJson = JsonSerializer.Serialize(this);
            newPlug = JsonSerializer.Deserialize<Plug>(tmpJson);
            newPlug.Id = null;
            return newPlug;
        }

        /// <summary>
        /// 获取插头的配置信息
        /// </summary>
        /// <returns></returns>
        public PlugSettings? GetPlugSettings()
        {
            if(string.IsNullOrEmpty(PlugSettingsJson))
            {
                return new PlugSettings();
            }
            var settings = JsonSerializer.Deserialize<PlugSettings>(PlugSettingsJson);
            return settings;
        }

        /// <summary>
        /// 设置插头配置的方便类
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetPlugSetting(string key,string value)
        {
            var PlugSettings = GetPlugSettings();
            PlugSettings.SetSetting(key,value);
            PlugSettingsJson = PlugSettings.GetSettingsJson();
        }

        /// <summary>
        /// 获取插头配置的方便类
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetPlugSetting(string key)
        {
            var PlugSettings = GetPlugSettings();
            return PlugSettings.GetSetting(key);
        }



        /// <summary>
        /// 设置插头的参数值方便类
        /// </summary>
        /// <param name="VariableName"></param>
        /// <param name="VariableValue"></param>
        public void SetVariableValue(string VariableName,string VariableValue)
        {
            bool VariableExist = PlugVariables.FirstOrDefault(p => p.Name == VariableName) != null;
            if (VariableExist)
            {
                PlugVariables.FirstOrDefault(p => p.Name == VariableName).Value = VariableValue;
            }
            else
            {
                PlugVariables.Add(new PlugVariable() { Name = VariableName, Value = VariableValue });
            }
        }

        /// <summary>
        /// 获取插头参数值的方便类
        /// </summary>
        /// <param name="VariableName"></param>
        /// <returns></returns>
        public string? GetVariableValue(string VariableName)
        {
            var Variable = PlugVariables?.FirstOrDefault(p => p.Name == VariableName);
            if (Variable != null)
            {
                return Variable.Value;
            }
            return null;
        }


        /// <summary>
        /// 添加插头参数，需要做唯一性校验
        /// </summary>
        /// <param name="VariableName"></param>
        public void AddVariable(string VariableName,string? Type,bool IsInitVariable=false)
        {
            if (string.IsNullOrEmpty(VariableName))
            {
                return;
            }
            foreach (var i in PlugVariables)
            {
                if (i.Name.Equals(VariableName, StringComparison.OrdinalIgnoreCase))
                {
                    i.IsInitVariable = IsInitVariable;
                    i.Type = Type ?? i.Type;
                    return;
                }
            }
            PlugVariables.Add(new PlugVariable() { Name = VariableName, Value = "",IsInitVariable= IsInitVariable, Type=Type });
        }



    }
}
