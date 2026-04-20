using CJ.Plug.Models.PlugProcess;
using Elsa.Studio.DomInterop.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace CJ.Plug.ElsaIntegration.Contracts
{
    public interface IElsaDomToolService
    {
        Task DownloadFileFromStreamAsync(string fileName, Stream stream);

        Task ClickElementAsync(ElementRef elementRef, CancellationToken cancellationToken = default);

        /// <summary>
        /// 导出流程为json
        /// </summary>
        /// <param name="workflowDefinition"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ExportAsync(Plug.Models.Plug.Plug Process, CancellationToken cancellationToken = default);
        Task ExportFromJsonDataAsync(string fileName, string JsonData, CancellationToken cancellationToken = default);

        Task ImportFromJsonAsync(IReadOnlyList<IBrowserFile> files,ImportOptions? options = null);
    }
}
