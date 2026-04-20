using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CJ.Plug.Models.EventAggregator.Notifications
{
    public record ActivitySelectedNotify(EventCallback<JsonObject> Callback)
    {
        public Task Invoke(JsonObject value) => Callback.InvokeAsync(value);
    }
}
