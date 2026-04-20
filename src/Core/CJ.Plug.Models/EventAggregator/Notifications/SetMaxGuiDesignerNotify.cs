using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.EventAggregator
{
    public record SetMaxGuiDesignerNotify(EventCallback<bool> Callback)
    {
        public Task Invoke(bool value) => Callback.InvokeAsync(value);
    }
}
