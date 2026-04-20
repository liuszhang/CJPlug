using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.EventAggregator.Notifications
{
    public record SetMenuNotify(EventCallback<bool> Callback)
    {
        public Task Invoke(bool value) => Callback.InvokeAsync(value);
    }
}
