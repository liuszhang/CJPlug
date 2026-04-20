public partial class PlugDataZone
    {        

        // 设置PDZ的参数值方便类
        public void SetActionVariableValue(string? PlugDefinitionId,string? ActionIdentityId, string? VariableName, string? VariableValue)
        {
            var VariableExist = ActionVariableDatas?
                .Where(p => p.PlugDefinitionId == PlugDefinitionId)
                .Where(p => p.ActionIdentityId == ActionIdentityId)
                .FirstOrDefault(p => p.Name == VariableName);
            if (VariableExist != null)
            {
                VariableExist.Value = VariableValue;
                //VariableExist.IsValueFromOtherVariable = false;
            }
            else
            {
                ActionVariableDatas.Add(new ActionVariableData() 
                { 
                    PlugDefinitionId = PlugDefinitionId,
                    ActionIdentityId = ActionIdentityId,
                    Name = VariableName, 
                    Value = VariableValue });
            }
        }

        // 获取PDZ参数值的方便类
        public string? GetActionVariableValue(string? ActionIdentityId, int? VariableId, string? VariableName)
        {
            string? resultValue = null;
            if (!string.IsNullOrEmpty(VariableName))
            {
                //Log.Information("1.5");
                resultValue = GetActionVariableValue(ActionIdentityId, VariableName);
            }
            else if (VariableId != null)
            {
                resultValue = GetActionVariableValue(ActionIdentityId, VariableId);
            }
            return resultValue;
        }

        // 获取PDZ参数值的方便类(通过参数名)
        public string? GetActionVariableValue(string ActionIdentityId, string VariableName)
        {
            var Variable = ActionVariableDatas?
                .Where(p => p.ActionIdentityId == ActionIdentityId)
                .FirstOrDefault(p => p.Name == VariableName);
            if (Variable != null)
            {
                //Log.Information(JsonSerializer.Serialize(Variable));
                if (Variable.IsValueFromOtherVariable && string.IsNullOrEmpty(Variable.Value))
                {
                    var Value = GetActionVariableValue(Variable.SourceValuePlugDefinitionId, Variable.SourceValueVariableId, Variable.SourceValueVariableName);
                    return Value;
                }
                return Variable.Value;
            }
            return null;
        }

        // 获取PDZ参数值的方便类（通过参数Id）
        public string? GetActionVariableValue(string? ActionIdentityId, int? VariableId)
        {
            var Variable = ActionVariableDatas?
                .Where(p => p.ActionIdentityId == ActionIdentityId)
                .FirstOrDefault(p => p.Id == VariableId);
            if (Variable != null)
            {
                if (Variable.IsValueFromOtherVariable)
                {
                    var Value = GetActionVariableValue(Variable.SourceValuePlugDefinitionId, Variable.SourceValueVariableId, Variable.SourceValueVariableName);
                    return Value;
                }
                return Variable.Value;
            }
            return null;
        }



    }

