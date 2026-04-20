using CJ.Plug_Aspire.Models.Plug;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Workflows;
using Elsa.Workflows.Activities.Flowchart.Attributes;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using NX.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

//[Activity("Demo6666", "Demo6666", "Simple activity6666666666666 ")]
//[Activity(Namespace = "Demo", Category = "Demo", DisplayName = "测试活动", Description = "A simple activity that writes \"Hello World!\" to the console.")]
[Activity("DemoNamespace",Namespace ="CJ",Description ="西门子NX软件",Category ="商业软件",DisplayName ="NX")]
public class NXPlug : CodeActivity<string>
{
    [Description("测试输入参数")]
    public Input<string> TestText { get; set; } = default!;

    //[Input(IsBrowsable =false,Description ="模型输入参数")]
    public Input<List<PlugVariable>> InputDatas { get; set; } = default!;

    public Output<List<PlugVariable>> OutputDatas { get; set; } = default!;

    public NXPlug()
    {
        
    }
    public NXPlug(Variable<string>? testText, Variable<List<PlugVariable>>? inputDatas, Variable<List<PlugVariable>>? outputDatas)
    {
        TestText = new(testText);
        InputDatas = new(inputDatas);
        OutputDatas = new(outputDatas);
    }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var text = context.Get(TestText);
        var InputData = context.Get(InputDatas);
        var OutputData = context.Get(OutputDatas);
        await new NXPlugExecute(text, InputData, OutputData).Execute();        

    }
}

