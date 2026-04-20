using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CJ.Plug.ElsaIntegration.Services
{
    public class ElsaNameGenerator
    {
        public static bool GetActivityNameExists(IEnumerable<JsonObject> activities, string name)
        {
            return activities.Any(x => x.GetName() == name);
        }

        /// <inheritdoc />
        public static string GenerateNextActivityName(IEnumerable<JsonObject> activities, ActivityDescriptor activityDescriptor)
        {
            var max = 10000;
            var enumerable = activities as JsonObject[] ?? activities.ToArray();
            var count = GetNextActivityNumber(enumerable, activityDescriptor);

            while (count++ < max)
            {
                var nextName = $"{activityDescriptor.Name}{count}";
                if (!GetActivityNameExists(enumerable, nextName))
                    return nextName;
            }

            throw new Exception("Could not generate a unique name.");
        }

        private static int GetNextActivityNumber(IEnumerable<JsonObject> activities, ActivityDescriptor activityDescriptor)
        {
            return activities.Count(x => x.GetTypeName() == activityDescriptor.TypeName);
        }
    }
}
