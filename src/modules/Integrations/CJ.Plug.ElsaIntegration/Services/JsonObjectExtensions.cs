using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Elsa.Api.Client.Extensions;

namespace CJ.Plug.ElsaIntegration.Services
{
    public static class JsonObjectExtensions
    {
        /// <summary>
        /// 将activity json转为Plug类
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static CJ.Plug.Models.Plug.Plug ToPlug(this JsonObject obj)
        {
            var newPlug=new CJ.Plug.Models.Plug.Plug();
            newPlug.DefinitionId=obj.GetId();
            newPlug.Name=obj.GetName();
            newPlug.Type=obj.GetTypeName();
            //newPlug.CoreType = obj.GetTypeName();
            newPlug.ActivityNodeId=obj.GetNodeId();
            newPlug.ActivityVersion=obj.GetVersion().ToString();
            newPlug.ActivityMetaData = obj.GetMetadata()?.ToString();
            newPlug.ActivityJsonData=obj.ToString();
            //newPlug.ProcessDefinitionId = obj.GetWorkflowDefinitionId();
            return newPlug;
        }

        public static string GetDisplayTypeName(this JsonObject activity) => activity.GetProperty<string>("displayType")!;


        public static JsonObject? UpdateActivity(this JsonObject Flowchart, JsonObject newActivity)
        {
            var activitiesArray = Flowchart.GetActivities().ToArray();

            // 找到目标Activity
            var targetActivityId = newActivity.GetId();
            var targetNode = activitiesArray.FirstOrDefault(
                node => node.GetId() == targetActivityId);

            // 更新Activity
            if (targetNode != null)
            {
                // 找到索引并替换
                var index = Array.IndexOf(activitiesArray,targetNode);
                activitiesArray[index] = newActivity;
            }
            Flowchart.SetActivities(activitiesArray.SerializeToArray());

            return Flowchart;
        }
    }
}
