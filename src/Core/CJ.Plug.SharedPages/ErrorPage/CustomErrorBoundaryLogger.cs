using CJ.Plug.SharedPages.ErrorPage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;

public class CustomErrorBoundaryLogger : IErrorBoundaryLogger
{
    private readonly ILogger<CustomErrorBoundaryLogger> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CustomErrorBoundaryLogger(ILogger<CustomErrorBoundaryLogger> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async ValueTask LogErrorAsync(Exception exception)
    {
        _logger.LogError(exception, "6868,An unhandled exception has occurred.");

        // 确保在正确的上下文中调用
        await Task.Run(async () =>
        {
            Console.WriteLine("------------CustomErrorBoundaryLogger--------------");
            using (var scope = _serviceProvider.CreateScope())
            {
                var dialogService = scope.ServiceProvider.GetRequiredService<IDialogService>();
                await dialogService.ShowAsync<DebugErrorMessagePage>();
            }
        });
    }
}