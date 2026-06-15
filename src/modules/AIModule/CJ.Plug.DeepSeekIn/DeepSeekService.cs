using System;
namespace CJ.Plug.DeekSeekIn
{
    public partial class DeepSeekService : IDeepSeekService
    {
        //public readonly MainApiClient MainApiclient;
        private readonly Uri _modelEndpoint = new Uri("http://localhost:11434");
        //private readonly string _modelName = "deepseek-r1:1.5b";
        //private readonly string _modelName = "qwen3:1.7b";
        private readonly string _modelName = "qwen3:4b";
    }
}
