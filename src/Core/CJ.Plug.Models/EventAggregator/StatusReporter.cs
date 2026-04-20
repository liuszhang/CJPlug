using CJ.Plug.Models.Extensions;
using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using Serilog;
using System.Text.Json;

namespace CJ.Plug.Models.EventAggregator
{
    public class StatusReporter
    {
        public static void ReportPlugStatus(string? DefinitionId, PlugStatus? plugStatus,string? PDZId="")
        {
            var statusString = JsonSerializer.Serialize(plugStatus);
            if (PDZId?.GetPDZType()==PDZTypeEnum.Job1.ToString())
            {
                //Log.Information(HubEventNameEnum.ActivityStatusNow.ToString() + $"|{PDZId.GetDesignPDZId()}|{DefinitionId}|{JsonSerializer.Serialize(plugStatus)}");
                CLog.Information(statusString, PDZId.GetDesignPDZId(), PDZId, DefinitionId, null, LogTypeEnum.ActivityStatusNow);
            }
            //Log.Information(HubEventNameEnum.ActivityStatusNow.ToString() + $"|{PDZId}|{DefinitionId}|{JsonSerializer.Serialize(plugStatus)}");
            CLog.Information(statusString, PDZId, PDZId, DefinitionId, null, LogTypeEnum.ActivityStatusNow);
        }

        //public static void ReportPlugStatus(string? DefinitionId, PlugStatusEnum? plugStatus)
        //{
        //    Log.Information($"{DefinitionId}|{JsonSerializer.Serialize(plugStatus)}");
        //}

        public static void ResumeProcess(string? InstanceId, string? DefinitionId,string? CorrelationId)
        {
            Log.Information("ResumeElsaProcess:" + InstanceId + "-" + DefinitionId+"-"+ CorrelationId);
        }

        public static void CompleteActivityContext(string? CorrelationId, string? DefinitionId)
        {
            //Log.Information("CompleteActivityContext:" + CorrelationId + DefinitionId);
            CLog.Information(CorrelationId + DefinitionId, null, null, null, null, LogTypeEnum.CompleteActivityContext);
            //Log.Information("ResumeElsaProcess:" + CorrelationId + "-" + DefinitionId);
        }

        // PDZ更新通知器，通知其他页面进行数据的重加载
        public static void PDZUpdated(string? PDZId)
        {
            //Log.Information("PDZUpdatedInfo:" + PDZId);
            CLog.Information(PDZId,PDZId,null,null,null,LogTypeEnum.PDZUpdatedInfo);
        }

        public static void PlugUpdated(string? PlugDefiniitonId)
        {
            CLog.Information(PlugDefiniitonId,null,null,null,null,LogTypeEnum.PlugUpdated);
        }

        public static void JobStatusUpdated(string? JobCorrelationId)
        {
            CLog.Information(JobCorrelationId,null,null,null,null,LogTypeEnum.JobStatusUpdated);
        }
    }
}
