using Microsoft.AspNetCore.Components;

namespace CJ.Plug.HomePage.Components.Landing;

public partial class StatisticsLine
{
    private int _userCount;
    private int _toolCount;
    private int _plugCount;
    private string _version = "1.0.0";
    private bool _loaded;

    [Inject] private MainApiClient ApiClient { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var users = await ApiClient.GetAllUsersAsync();
            _userCount = users?.Count() ?? 0;

            var tools = await ApiClient.GetAllToolsAsync();
            _toolCount = tools?.Count ?? 0;

            var plugs = await ApiClient.GetPlugs();
            _plugCount = plugs?.Count ?? 0;

            _loaded = true;
        }
        catch
        {
            _loaded = true;
        }
    }

}
