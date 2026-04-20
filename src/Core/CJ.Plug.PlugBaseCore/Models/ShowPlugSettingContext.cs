using CJ.Plug.Models.EventAggregator.Notifications;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CJ.Plug.PlugBaseCore.Models
{
    public record ShowPlugSettingContext(JsonObject? Activity, PlugDataZone? PlugDataZone,SetMenuNotify? SetMenuNotify=null);
}
