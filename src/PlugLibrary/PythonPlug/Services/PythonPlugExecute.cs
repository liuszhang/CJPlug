using CJ.Plug.Models.Plug;
using Microsoft.AspNetCore.Components.Forms;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PythonPlug.Services
{
    public class PythonPlugExecute
    {

        string? text;
        List<PlugVariable>? InputData;
        List<PlugVariable>? OutputData;

        public PythonPlugExecute(string? text, List<PlugVariable>? inputData, List<PlugVariable>? outputData)
        {
            this.text = text;
            InputData = inputData;
            OutputData = outputData;
        }

        public async Task Execute()
        {
            Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>Hello Python!:" + text);
            Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>activity has input:" + InputData?.Count);
            foreach (var i in InputData)
            {
                Console.WriteLine($"{i.Name}={i.Value}");
            }
            Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>input:" + JsonSerializer.Serialize(InputData));
        }
    }
}
