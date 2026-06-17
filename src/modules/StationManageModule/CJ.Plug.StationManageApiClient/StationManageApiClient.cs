namespace CJ.Plug.StationManageApiClient
{
    public partial class StationManageApiClient : BaseApiClient, IStationManageApiClient
    {
        public StationManageApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
        {
        }
    }
}
