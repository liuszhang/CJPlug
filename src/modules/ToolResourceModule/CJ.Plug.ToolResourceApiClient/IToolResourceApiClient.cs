using System.IO;
using CJ.Plug.Models.Station;

namespace CJ.Plug.ToolResourceApiClient
{
    public interface IToolResourceApiClient
    {
        // Tool CRUD
        Task<List<Tool>?> GetAllToolsAsync(CancellationToken cancellationToken = default);
        Task<Tool?> GetToolByIdAsync(int? id, CancellationToken cancellationToken = default);
        Task<Tool?> GetToolByDisplayNameAsync(string? toolDisplayName, CancellationToken cancellationToken = default);
        Task<Tool?> CreateToolAsync(Tool newTool, CancellationToken cancellationToken = default);
        Task<Tool?> UpdateToolAsync(Tool updatedTool, CancellationToken cancellationToken = default);
        Task<bool> DeleteToolAsync(int? ToolId, CancellationToken cancellationToken = default);

        // Tool file management
        Task<Stream> DownloadToolAsync(string toolName, string version, CancellationToken cancellationToken = default);
        Task<bool> MoveToolFilesFromTmpAsync(string toolName, bool isSystemTool, string userName);
        Task<bool> DeleteToolTmpFilesAsync();

        // Import
        Task<int> ImportDefaultToolsAsync(CancellationToken cancellationToken = default);
    }
}
