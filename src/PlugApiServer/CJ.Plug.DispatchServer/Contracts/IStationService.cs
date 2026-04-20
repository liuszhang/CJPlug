namespace CJ.Plug.DispatchServer.Contracts
{
    public interface IStationService
    {
        Task<string?> GetStationToExecute();
        Task<List<string>?> GetAllOnlineStation();
        Task<string?> GetApiServer();
        Task<string?> GetElsaEngineServer();
        Task<string?> GetElsaEngineApiKey();

    }
}
