using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Extensions
{
    public static class JsonExtensions
    {
        /// <summary>
        /// Gets the property value of the specified model.
        /// </summary>
        /// <param name="model">The model to get the property value from.</param>
        /// <param name="path">The path to the property.</param>
        /// <returns>The property value.</returns>
        public static JsonNode? GetProperty(this JsonObject model, params string[] path)
        {
            var currentModel = model;

            foreach (var prop in path.SkipLast(1))
            {
                if (currentModel[prop] is not JsonObject value)
                    return default;

                currentModel = value;
            }

            return currentModel[path.Last()];
        }

        public static IEnumerable<JsonObject> ChildPlugJsons(this JsonObject flowchart)
        {
            return flowchart.GetProperty("activities")?.AsArray().AsEnumerable().Cast<JsonObject>() ?? Array.Empty<JsonObject>();
        }

        public static bool? GetIsContainer(this JsonObject activity)
        {
            //Console.WriteLine($"activity:{activity}");
            //activity.TryGetPropertyValue("isContainer", out var isContainer);

            //Console.WriteLine($"GetIsContainer:{isContainer}");
            //Console.WriteLine($"GetIsContainer:{activity.GetProperty("isContainer")?.GetValue<bool>()}");
            return activity.GetProperty("isContainer")?.GetValue<bool>();
            //return isContainer?.ToString();
        }


        

    }
}
