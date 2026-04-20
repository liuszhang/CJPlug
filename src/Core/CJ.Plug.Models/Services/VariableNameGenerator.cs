using CJ.Plug.Models.Shared;

namespace CJ.Plug.Models.Services
{
    public class VariableNameGenerator
    {
        private static bool GetVariableNameExists(List<BaseVariable> variables, string name)
        {
            return variables.Any(x => x.Name == name);
        }

        /// <inheritdoc />
        private static string GenerateNextVariableName(List<BaseVariable> variables)
        {
            var max = 1000;
            var count = 1;

            while (count++ < max)
            {
                var nextName = $"p{count}";
                if (!GetVariableNameExists(variables, nextName))
                    return nextName;
            }

            throw new Exception("Could not generate a unique name.");
        }

        //public static string GetNextVariableName(List<PDZVariable> variables)
        //{
        //    var baseVariables = variables.OfType<BaseVariable>().ToList();
        //    var VaraibleName = VariableNameGenerator.GenerateNextVariableName(baseVariables);
        //    return VaraibleName;
        //}

        //public static string GetNextVariableName(List<PlugVariable> variables)
        //{
        //    var baseVariables = variables.OfType<BaseVariable>().ToList();
        //    var VaraibleName = VariableNameGenerator.GenerateNextVariableName(baseVariables);
        //    return VaraibleName;
        //}

        public static string GetNextVariableName<T>(List<T> variables) where T : BaseVariable // 泛型约束：T 必须是 BaseVariable 或其子类
        {
            var baseVariables = variables.Cast<BaseVariable>().ToList();
            var variableName = VariableNameGenerator.GenerateNextVariableName(baseVariables);
            return variableName;
        }
    }
}
