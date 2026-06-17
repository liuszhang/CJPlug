
using CJ.Plug.Models.Station;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public interface IToolManageService
    {
        Task<IEnumerable<Tool>?> GetAllToolsAsync(CancellationToken cancellationToken = default);
        Task<Tool?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Tool?> GetByDisplayNameAsync(string ToolDisplayName, CancellationToken cancellationToken = default);
        Task<Tool?> CreateToolAsync(Tool newTool, CancellationToken cancellationToken = default);
        Task<bool> DeleteToolAsync(int ToolId, CancellationToken cancellationToken = default);
        Task<Tool?> UpdateToolAsync(Tool updatedTool, CancellationToken cancellationToken = default);
        Task<int> ImportDefaultToolsAsync(CancellationToken cancellationToken = default);

        Task<bool> MoveToolFilesFromTmpAsync(string toolName, bool isSystemTool, string userName);
        Task<bool> DeleteToolTmpFilesAsync();
    }

