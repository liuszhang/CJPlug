using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.SharedPages.Contracts
{
    public interface IDomInteropService
    {
        Task<string?> GetCurrentUserName();
        Task<int?> GetCurrentUserId();
        Task SetCurrentUserId(int id);
        Task SetCurrentUserName(string userName);
        Task ClearCurrentUser();
        Task<string?> GetItemValue(string key);
        Task SetItemValue(string key,string value);

        Task CopyText(string text, CancellationToken cancellationToken = default);
        Task SetDragPayload(object? value);
        Task<object?> GetDragPayload();
        Task<string?> GetPDZId();
    }
}
