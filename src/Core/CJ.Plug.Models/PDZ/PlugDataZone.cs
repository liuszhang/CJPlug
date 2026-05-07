using CJ.Plug.Models.DataFlow;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using System.Text.Json;
using System.Text.Json.Nodes;


public partial class PlugDataZone
    {        

        // 设置PDZ的参数值方便类
        public PlugVariableData? SetVariableValue(string? PlugDefinitionId, string? VariableName, string? VariableValue, string? VariableType = null)
        {
            var VariableExist = PlugVariableDatas?
                .Where(p => p.PlugDefinitionId == PlugDefinitionId)
                .FirstOrDefault(p => p.Name == VariableName);
            if (VariableExist != null)
            {
                VariableExist.Value = VariableValue;
                if (!string.IsNullOrEmpty(VariableType))
                {
                    VariableExist.Type = VariableType;
                }
                return VariableExist;
            }
            else
            {
            var newVariable = new PlugVariableData() { PlugDataZoneId = this.Id, PlugDefinitionId = PlugDefinitionId, Name = VariableName, Value = VariableValue, Type = VariableType ?? CJ.Plug.Models.VariableType.VariableTypeEnum.String.ToString() };
            PlugVariableDatas.Add(newVariable);
            return newVariable;
        }
        }

        // 获取PDZ参数值的方便类
        public string? GetVariableValue(string? PlugDefinitionId, int? VariableId, string? VariableName)
        {
            string? resultValue = null;
            if (!string.IsNullOrEmpty(VariableName))
            {
                //Log.Information("1.5");
                resultValue = GetVariableValue(PlugDefinitionId, VariableName);
            }
            else if (VariableId != null)
            {
                resultValue = GetVariableValue(PlugDefinitionId, VariableId);
            }
            return resultValue;
        }

        // 获取PDZ参数值的方便类(通过参数名)
        public string? GetVariableValue(string PlugDefinitionId, string VariableName)
        {
            var Variable = PlugVariableDatas?
                .Where(p => p.PlugDefinitionId == PlugDefinitionId)
                .FirstOrDefault(p => p.Name == VariableName);
            if (Variable != null)
            {
                //Log.Information(JsonSerializer.Serialize(Variable));
                if (Variable.IsValueFromOtherVariable && string.IsNullOrEmpty(Variable.Value))
                {
                    //Log.Information($"1:{Variable.SourceValuePlugDefinitionId}-{Variable.SourceValueVariableId}-{Variable.SourceValueVariableName}");
                    var Value = GetVariableValue(Variable.SourceValuePlugDefinitionId, Variable.SourceValueVariableId, Variable.SourceValueVariableName);
                    //Log.Information("2:"+Value);
                    return Value;
                }
                return Variable.Value;
            }
            return null;
        }

        // 获取PDZ参数值的方便类（通过参数Id）
        public string? GetVariableValue(string? PlugDefinitionId, int? VariableId)
        {
            var Variable = PlugVariableDatas?
                .Where(p => p.PlugDefinitionId == PlugDefinitionId)
                .FirstOrDefault(p => p.Id == VariableId);
            if (Variable != null)
            {
                if (Variable.IsValueFromOtherVariable)
                {
                    //Log.Information($"1.6:{Variable.SourceValuePlugDefinitionId}-{Variable.SourceValueVariableId}-{Variable.SourceValueVariableName}");

                    var Value = GetVariableValue(Variable.SourceValuePlugDefinitionId, Variable.SourceValueVariableId, Variable.SourceValueVariableName);
                    //Log.Information("1.7:" + Value);
                    return Value;
                }
                //Log.Information("1.8:" + Variable.Value);
                return Variable.Value;
            }
            return null;
        }

        // 将插头的参数复制到PDZ参数中
        public void SetVariablesFromPlug(Plug plug)
        {
            foreach (var v in plug.PlugVariables)
            {
                SetVariableValue(plug.DefinitionId, v.Name, v.Value, v.Type);
            }
            // 流程图数据已迁移至插头定义层，不再复制到 PDZ
            // SetFlowchartData(plug.DefinitionId, plug.ToActivityJson());
        }

        //获取PDZ中指定插头的参数列表,PlugDefinitionId为空时获取所有参数
        public List<PlugVariableData> GetVariablesOfPlug(string? PlugDefinitionId = null)
        {
            if (string.IsNullOrEmpty(PlugDefinitionId))
            {
                return PlugVariableDatas?
                .Where(p => p.IsBrowsable == true)
                .ToList() ?? new List<PlugVariableData>();
            }
            return PlugVariableDatas?
                .Where(p => p.PlugDefinitionId == PlugDefinitionId)
                .Where(p => p.IsBrowsable == true)
                .ToList() ?? new List<PlugVariableData>();
        }

        /// <summary>
        /// 获取PDZ中指定插头的参数列表，如果PDZ中没有该插头的参数，则从插头定义中回退获取自带参数（值为空）
        /// </summary>
        public List<BaseVariable> GetVariablesOfPlugWithFallback(string? PlugDefinitionId, Plug? plug = null)
        {
            var pdzVariables = GetVariablesOfPlug(PlugDefinitionId);
            if (pdzVariables.Count > 0)
            {
                return pdzVariables.Cast<BaseVariable>().ToList();
            }

            // PDZ中没有该插头的参数，从插头定义中获取自带参数
            if (plug?.PlugVariables == null || plug.PlugVariables.Count == 0)
            {
                return new List<BaseVariable>();
            }

            return plug.PlugVariables
                .Where(v => v.IsBrowsable != false)
                .Cast<BaseVariable>()
                .ToList();
        }

        /// <summary>
        /// 将插头定义中的参数同步到PDZ中（仅同步PDZ中不存在的参数）
        /// </summary>
        public void SyncVariablesFromPlug(Plug plug)
        {
            if (plug?.PlugVariables == null) return;

            foreach (var plugVar in plug.PlugVariables)
            {
                var existing = PlugVariableDatas?
                    .FirstOrDefault(p => p.PlugDefinitionId == plug.DefinitionId && p.Name == plugVar.Name);
                if (existing != null) continue;

                var newPlugVariableData = new PlugVariableData()
                {
                    PlugDefinitionId = plug.DefinitionId,
                    Name = plugVar.Name,
                    Type = plugVar.Type,
                    Value = null, // 值为空
                    IsInitVariable = plugVar.IsInitVariable,
                    IsBrowsable = plugVar.IsBrowsable,
                };
                PlugVariableDatas ??= new List<PlugVariableData>();
                PlugVariableDatas.Add(newPlugVariableData);
            }
        }

        //获取PDZ中指定插头的动作数据
        public List<ActionData>? GetActionDatasOfPlug(string PlugDefinitionId)
        {
            return ActionDatas?
                .Where(p => p.PlugDefinitionId == PlugDefinitionId)
                .ToList();
        }


        //获取指定插头的流程图数据
        [Obsolete("请使用 Plug.GetFlowchartJson() 代替。流程图数据已迁移至插头定义层。")]
        public JsonObject? GetFlowchartData(string PlugDefinitionId)
        {
            var ActivityJsonData = FlowchartDatas?
                .Where(p => p.PlugDefinitionId == PlugDefinitionId)
                .FirstOrDefault();
            if (!string.IsNullOrEmpty(ActivityJsonData?.Value))
            {
                // 方法1：动态反序列化
                var rootDynamic = JsonNode.Parse(ActivityJsonData?.Value)?.AsObject();

                return rootDynamic;
            }
            return null;
        }

        // 保存流程图数据
        [Obsolete("请使用 Plug.SetFlowchartJson() 代替。流程图数据已迁移至插头定义层。")]
        public FlowchartData SetFlowchartData(string? PlugDefinitionId, JsonObject JsonData)
        {
            if (string.IsNullOrEmpty(PlugDefinitionId))
            {
                PlugDefinitionId = this.PlugDefinitionId;
            }
            var VariableExist = FlowchartDatas?.FirstOrDefault(p => p.PlugDefinitionId == PlugDefinitionId);
            if (VariableExist != null)
            {
                VariableExist.Value = JsonData.ToJsonString();
                return VariableExist;
            }
            else
            {
                var newFlowchartData = new FlowchartData()
                {
                    PlugDataZoneId = this.Id,
                    PlugDefinitionId = PlugDefinitionId,
                    Value = JsonData.ToJsonString()
                };
                FlowchartDatas?.Add(newFlowchartData);
                return newFlowchartData;
            }
        }


        //设置指定插头的状态数据
        public void SetPlugStatusData(string PlugDefinitionId, PlugStatus plugStatus)
        {
            var VariableExist = PDZVariables
                .Where(p => p.PlugDefinitionId == PlugDefinitionId)
                .FirstOrDefault(p => p.Tag == PDZVariableTagEnum.PlugStatusTag.ToString());
            if (VariableExist != null)
            {
                VariableExist.Value = JsonSerializer.Serialize(plugStatus);
            }
            else
            {
                PDZVariables.Add(new PDZVariable()
                {
                    PlugDefinitionId = PlugDefinitionId,
                    Tag = PDZVariableTagEnum.PlugStatusTag.ToString(),
                    Value = JsonSerializer.Serialize(plugStatus)
                });
            }
        }

        //复制PDZ，根据目标PDZ类型不同可能会有不同的处理逻辑
        public PlugDataZone CopyPDZ(string? UserName, string? TargetPDZType, string? JobDefinitionId)
        {
            var newPDZ = new PlugDataZone();
            newPDZ.UserName = UserName;
            newPDZ.PlugDefinitionId = PlugDefinitionId;
            newPDZ.Type = TargetPDZType ?? this.Type;
            newPDZ.JobDefinitionId = JobDefinitionId;
            // 与 GetOrCreatePDZFromPlugDefinitionId 保持一致的 PDZId 格式
            if (newPDZ.Type == PDZTypeEnum.DesignPDZ.ToString() || newPDZ.Type == PDZTypeEnum.Desi.ToString())
            {
                newPDZ.PDZId = UserName + PlugDefinitionId + PDZTypeEnum.Desi.ToString();
            }
            else
            {
                newPDZ.PDZId = UserName + PlugDefinitionId + TargetPDZType + JobDefinitionId;
            }
            newPDZ.TriggerPlugDefinitionId=this.TriggerPlugDefinitionId;
            //newPDZ.PDZWorkPath = Path.Combine(UserName, UserName + PlugDefinitionId + TargetPDZType + JobDefinitionId);
            foreach (var v in PDZVariables)
            {
                var newVariable = new PDZVariable()
                {
                    PlugDefinitionId = v.PlugDefinitionId,
                    Name = v.Name,
                    Value = v.Value,
                    DisplayValue = v.DisplayValue,
                    Tag = v.Tag,
                    SN = v.SN,
                    IsInitVariable = v.IsInitVariable,
                    ActionIdentityId = v.ActionIdentityId,
                    PlugActionType = v.PlugActionType,
                    PlugActionDefinitionId = v.PlugActionDefinitionId,
                    IsBrowsable = v.IsBrowsable,
                    IsValueFromOtherVariable = v.IsValueFromOtherVariable,
                    SourceValuePlugDefinitionId = v.SourceValuePlugDefinitionId,
                    SourceValueVariableId = v.SourceValueVariableId,
                    SourceValueVariableName = v.SourceValueVariableName
                };
                if (v.Tag == PDZVariableTagEnum.PDZVariable.ToString() && TargetPDZType == PDZTypeEnum.Desi.ToString())
                {
                    newVariable.Value = null;
                }
                if (v.Tag == PDZVariableTagEnum.PDZVariable.ToString() && v.IsValueFromOtherVariable)
                {
                    newVariable.Value = null;
                }

                newPDZ.PDZVariables.Add(newVariable);
            }
            foreach(var v in PlugVariableDatas)
            {
                var newPlugVariable = new PlugVariableData()
                {
                    PlugDefinitionId = v.PlugDefinitionId,
                    Name = v.Name,
                    Value = v.Value,
                    Type = v.Type,
                    DisplayValue = v.DisplayValue,
                    IsInitVariable = v.IsInitVariable,
                    IsBrowsable = v.IsBrowsable,
                    IsValueFromOtherVariable = v.IsValueFromOtherVariable,
                    SourceValuePlugDefinitionId = v.SourceValuePlugDefinitionId,
                    SourceValueVariableId = v.SourceValueVariableId,
                    SourceValueVariableName = v.SourceValueVariableName
                };
                if (TargetPDZType == PDZTypeEnum.Desi.ToString()|| v.IsValueFromOtherVariable)
                {
                    newPlugVariable.Value = null;
                }
                newPDZ.PlugVariableDatas.Add(newPlugVariable);
            }
            foreach (var v in PlugDatas)
            {
                var newPlugData = new PlugData()
                {
                    PlugDefinitionId = v.PlugDefinitionId,
                    Value = v.Value,
                    Name = v.Name,
                    DisplayValue = v.DisplayValue,
                    IsBrowsable = v.IsBrowsable,
                    Type = v.Type,
                    PlugType = v.PlugType,
                    PlugTypeKey = v.PlugTypeKey,
                    OnlyExecuteAction = v.OnlyExecuteAction,
                    ParentPlugDefinitionId = v.ParentPlugDefinitionId,
                    Creater = v.Creater,
                    Category = v.Category,
                    WorkPath = v.WorkPath,
                    Description = v.Description,
                    PlugPosition = v.PlugPosition,
                };
                newPDZ.PlugDatas.Add(newPlugData);
            }
            foreach (var v in PlugStatusDatas)
            {
                var newPlugStatus = new PlugStatusData()
                {
                    PlugDefinitionId = v.PlugDefinitionId,
                    Value = v.Value
                };
                newPDZ.PlugStatusDatas.Add(newPlugStatus);
            }
            foreach (var v in ActionDatas)
            {
                var newActionData = new ActionData()
                {
                    PlugDefinitionId = v.PlugDefinitionId,
                    ActionIdentityId = v.ActionIdentityId,
                    ActionPlugType = v.ActionPlugType,
                    ActionPlugRootDefinitionId = v.ActionPlugRootDefinitionId,
                    Value = v.Value
                };
                newPDZ.ActionDatas.Add(newActionData);
            }
            foreach (var v in ActionVariableDatas)
            {
                var newActionVariableData = new ActionVariableData()
                {
                    PlugDefinitionId = v.PlugDefinitionId,
                    Name = v.Name,
                    Value = v.Value,
                    Type = v.Type,
                    DisplayValue = v.DisplayValue,
                    IsInitVariable = v.IsInitVariable,
                    IsBrowsable = v.IsBrowsable,
                    IsValueFromOtherVariable = v.IsValueFromOtherVariable,
                    SourceValuePlugDefinitionId = v.SourceValuePlugDefinitionId,
                    SourceValueVariableId = v.SourceValueVariableId,
                    SourceValueVariableName = v.SourceValueVariableName
                };
                if (TargetPDZType == PDZTypeEnum.Desi.ToString() || v.IsValueFromOtherVariable)
                {
                    newActionVariableData.Value = null;
                }
                newPDZ.ActionVariableDatas.Add(newActionVariableData);
            }
            // 流程图数据已迁移至插头定义层，不再复制到 PDZ
            // foreach (var v in FlowchartDatas)
            // {
            //     var newFlowchartData = new FlowchartData()
            //     {
            //         PlugDefinitionId = v.PlugDefinitionId,
            //         Value = v.Value
            //     };
            //     newPDZ.FlowchartDatas.Add(newFlowchartData);
            // }
            foreach (var v in DataFlowDatas)
            {
                var newDataFlowData = new DataFlowData()
                {
                    SourcePlugDefinitionId = v.SourcePlugDefinitionId,
                    SourceVariableId = v.SourceVariableId,
                    SourceVariableName = v.SourceVariableName,
                    TargetPlugDefinitionId = v.TargetPlugDefinitionId,
                    TargetVariableId = v.TargetVariableId,
                    TargetVariableName = v.TargetVariableName,
                    PortLinkData = v.PortLinkData,
                };
                newPDZ.DataFlowDatas.Add(newDataFlowData);
            }
            return newPDZ;
        }


        //获取数据流数据
        public List<string?>? GetDataFlowData()
        {
            var DataFlows = DataFlowDatas?.FirstOrDefault();
            if (DataFlows!=null)
            {
                var dataFlowList = JsonSerializer.Deserialize<List<string?>>(DataFlows.Value);
                return dataFlowList;
            }
            return new List<string?>();
        }

        //设置数据流数据
        public void SetDataFlowData(List<string?> DataFlows)
        {
            var VariableExist = PDZVariables?
                .Where(p => p.Tag == PDZVariableTagEnum.DataFlowTag.ToString())
                .FirstOrDefault();
            if (VariableExist != null)
            {
                VariableExist.Value = JsonSerializer.Serialize(DataFlows);
            }
            else
            {
                PDZVariables?.Add(new PDZVariable() { Tag = PDZVariableTagEnum.DataFlowTag.ToString(), Value = JsonSerializer.Serialize(DataFlows) });
            }

            //还需要同步更新参数引用关系
            foreach(var v in PDZVariables)
            {
                v.IsValueFromOtherVariable = false;
            }
            foreach(var dataFlow in DataFlows)
            {
                var flowData = JsonSerializer.Deserialize<PortLinkModel>(dataFlow);
                var variable = PDZVariables?
                    .Where(p=>p.Tag==PDZVariableTagEnum.PDZVariable.ToString())
                    .Where(p => p.PlugDefinitionId == flowData.TargetPort.PlugDefinitionId)
                    .Where(p => p.Name == flowData.TargetPort.VariableName)
                    .FirstOrDefault();
                variable.IsValueFromOtherVariable = true;
                variable.SourceValuePlugDefinitionId = flowData.SourcePort.PlugDefinitionId;
                //variable.SourceValueVariableId = null; //数据流中没有Id
                variable.SourceValueVariableName = flowData.SourcePort.VariableName;
                variable.Value = null;
                variable.DisplayValue= $"{flowData.SourcePort.PlugDefinitionId}:{flowData.SourcePort.VariableName}"; //显示值为来源插头和参数名称
            }


        } 

        //初始化数据流数据
        public void GenerateDataFlowData()
        {
            var flowList=new List<string>();
            foreach(var v in PDZVariables)
            {
                if (v.Tag == PDZVariableTagEnum.PDZVariable.ToString() && v.IsValueFromOtherVariable)
                {
                    var flowData = new PortLinkModel();
                    flowData.SourcePort=new PortIdentifierModel(v.SourceValuePlugDefinitionId, v.SourceValueVariableId, v.SourceValueVariableName, PortTypeEnum.Out.ToString());
                    flowData.TargetPort=new PortIdentifierModel(v.PlugDefinitionId, v.Id, v.Name, PortTypeEnum.In.ToString());
                    flowList.Add(JsonSerializer.Serialize(flowData));
                }
            }
            SetDataFlowData(flowList);
        }




    }

