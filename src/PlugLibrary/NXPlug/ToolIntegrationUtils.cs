using CJ.Plug.Models.LogModels;

using CJ.Plug.Models.Plug;
using CJ.Plug.Models.VariableType;
using MudBlazor;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NXPlug
{
    public class ToolIntegrationUtils
    {

        /// <summary>
        /// 从NX插件获取到的参数字符串中解析出参数列表
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static List<ModelParameter>? ProcessGetParameters(string? parameters, List<ModelParameter>? OldModelParameters=null)
        {
            if (String.IsNullOrEmpty(parameters))
            {
                Log.Information("模型无参数");                
                return null;
            }
            Log.Information("获取到的模型参数：" + parameters);
            parameters = parameters.Trim().TrimStart('"').TrimEnd('"');
            parameters = parameters.Trim(' ');
            string[] paras = parameters.Split(' ');
            var ModelParameters = new List<ModelParameter>();
            foreach (var i in paras)
            {
                try
                {
                    var tmpNXInput = new ModelParameter { Name = i.Split('=')[0], Value = i.Split('=')[1], SettingValue = null, IsInput = false };
                    ModelParameters.Add(tmpNXInput);
                }
                catch (Exception ex)
                {
                    CLog.Error(ex.Message);
                    CLog.Error(ex.StackTrace);
                }
            };
            //将原来的参数输入输出配置合并到新的参数列表中
            foreach (var old in OldModelParameters)
            {
                var modelParameter = ModelParameters.FirstOrDefault(M => M.Name == old.Name);
                //如果参数列表中没有这个参数，说明新模型没有这个参数，则跳过
                if (modelParameter == null)
                {
                    continue;
                }
                if (old.IsValueFromOtherVariable)
                {

                    modelParameter.IsValueFromOtherVariable = old.IsValueFromOtherVariable;
                    modelParameter.SettingValue = old.SettingValue;
                }
                if (!string.IsNullOrEmpty(old.OutToVariable))
                {
                    modelParameter.OutToVariable = old.OutToVariable;
                }
            }
            return ModelParameters;
        }

        public static string? ProcessSetParameters(List<ModelParameter>? ModelParameters,string? PlugDefinitionId=null,PlugDataZone? PlugDataZone=null)
        {
            if (ModelParameters == null || ModelParameters.Count == 0)
            {
                Log.Information("模型无参数");
                return null;
            }

            string oldParameters = "";
            string newParameters = "";
            foreach (var input in ModelParameters)
            {
                oldParameters += input.Name + "=" + input.Value + ",";

                if (!input.IsValueFromOtherVariable)
                {
                    if (string.IsNullOrEmpty(input.SettingValue))
                    {
                        newParameters += input.Name + "=" + input.Value + ",";
                    }
                    else
                    {
                        newParameters += input.Name + "=" + input.SettingValue + ",";
                    }
                }
                else
                {
                    var settingValue = PlugDataZone.GetVariableValue(PlugDefinitionId,input.SettingValue);
                    CLog.Information("获取到的变量值：" + settingValue);
                    if (string.IsNullOrEmpty(settingValue))
                    {
                        CLog.Information("变量值为空，使用默认值");
                        settingValue = input.Value;
                    }
                    newParameters += input.Name + "=" + settingValue + ",";
                }   
            }
            if (oldParameters == newParameters)
            {
                CLog.Information("参数无变化");
                return null;
            }
            newParameters = newParameters.TrimEnd(',');
            CLog.Information(oldParameters);
            CLog.Information(newParameters);
            return newParameters;
        }
    }
}
