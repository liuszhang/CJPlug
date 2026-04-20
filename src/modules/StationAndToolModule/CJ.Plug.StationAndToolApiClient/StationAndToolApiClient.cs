namespace CJ.Plug.StationAndToolApiClient
{
    public partial class StationAndToolApiClient : BaseApiClient, IStationAndToolApiClient
    {
        public StationAndToolApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
        {
        }
    }
}
