namespace CJ.Plug.ToolResourceApiClient
{
    public partial class ToolResourceApiClient : BaseApiClient, IToolResourceApiClient
    {
        public ToolResourceApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
        {
        }
    }
}
