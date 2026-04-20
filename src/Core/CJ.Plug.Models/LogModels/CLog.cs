using CJ.Plug.Models.Extensions;
using Serilog;

namespace CJ.Plug.Models.LogModels
{
    public class CLog
    {

        public static void Information(
            string? LogContent, 
            string? Receiver=null,  
            string? PDZId=null,
            string? PlugDefinitionId=null,
            string? JobCorrelationId = null,
            LogTypeEnum? LogType = LogTypeEnum.CommonLog)
        {
            Log.ForContext(LogContextEnum.Receiver.ToString(), Receiver)
                .ForContext(LogContextEnum.PDZId.ToString(), PDZId)
                .ForContext(LogContextEnum.PlugDefinitionId.ToString(), PlugDefinitionId)
                .ForContext(LogContextEnum.JobCorrelationId.ToString(), JobCorrelationId)
                .ForContext(LogContextEnum.LogType.ToString(), LogType.ToString())
                .Information($"{LogContent}");

            //如果是流程图界面启动的作业，日志同时打印到流程图所在的设计PDZ中
            if (Receiver?.GetPDZType()==PDZTypeEnum.Job1.ToString())
            {
                var designPDZReceiver = Receiver.GetDesignPDZId();
                Log.ForContext(LogContextEnum.Receiver.ToString(), designPDZReceiver)
                    .ForContext(LogContextEnum.PDZId.ToString(), PDZId)
                    .ForContext(LogContextEnum.PlugDefinitionId.ToString(), PlugDefinitionId)
                    .ForContext(LogContextEnum.JobCorrelationId.ToString(), JobCorrelationId)
                    .ForContext(LogContextEnum.LogType.ToString(), LogType.ToString())
                    .Information($"{LogContent}");
            }
        }

        public static void Warning(
            string? LogContent,
            string? Receiver = null,
            string? PDZId = null,
            string? PlugDefinitionId = null,
            string? JobCorrelationId = null,
            LogTypeEnum? LogType = LogTypeEnum.CommonLog)
        {
            Log.ForContext(LogContextEnum.Receiver.ToString(), Receiver)
                .ForContext(LogContextEnum.PDZId.ToString(), PDZId)
                .ForContext(LogContextEnum.PlugDefinitionId.ToString(), PlugDefinitionId)
                .ForContext(LogContextEnum.JobCorrelationId.ToString(), JobCorrelationId)
                .ForContext(LogContextEnum.LogType.ToString(), LogType.ToString())
                .Warning($"{LogContent}");

            //如果是流程图界面启动的作业，日志同时打印到流程图所在的设计PDZ中
            if (Receiver?.GetPDZType() == PDZTypeEnum.Job1.ToString())
            {
                var designPDZReceiver = Receiver.GetDesignPDZId();
                Log.ForContext(LogContextEnum.Receiver.ToString(), designPDZReceiver)
                    .ForContext(LogContextEnum.PDZId.ToString(), PDZId)
                    .ForContext(LogContextEnum.PlugDefinitionId.ToString(), PlugDefinitionId)
                    .ForContext(LogContextEnum.JobCorrelationId.ToString(), JobCorrelationId)
                    .ForContext(LogContextEnum.LogType.ToString(), LogType.ToString())
                    .Information($"{LogContent}");
            }
        }

        public static void Error(
            string? LogContent,
            string? Receiver = null,
            string? PDZId = null,
            string? PlugDefinitionId = null,
            string? JobCorrelationId = null,
            LogTypeEnum? LogType = LogTypeEnum.CommonLog)
        {
            Log.ForContext(LogContextEnum.Receiver.ToString(), Receiver)
                .ForContext(LogContextEnum.PDZId.ToString(), PDZId)
                .ForContext(LogContextEnum.PlugDefinitionId.ToString(), PlugDefinitionId)
                .ForContext(LogContextEnum.JobCorrelationId.ToString(), JobCorrelationId)
                .ForContext(LogContextEnum.LogType.ToString(), LogType.ToString())
                .Error($"{LogContent}");

            //如果是流程图界面启动的作业，日志同时打印到流程图所在的设计PDZ中
            if (Receiver?.GetPDZType() == PDZTypeEnum.Job1.ToString())
            {
                var designPDZReceiver = Receiver.GetDesignPDZId();
                Log.ForContext(LogContextEnum.Receiver.ToString(), designPDZReceiver)
                    .ForContext(LogContextEnum.PDZId.ToString(), PDZId)
                    .ForContext(LogContextEnum.PlugDefinitionId.ToString(), PlugDefinitionId)
                    .ForContext(LogContextEnum.JobCorrelationId.ToString(), JobCorrelationId)
                    .ForContext(LogContextEnum.LogType.ToString(), LogType.ToString())
                    .Error($"{LogContent}");
            }
        }

    }
}
