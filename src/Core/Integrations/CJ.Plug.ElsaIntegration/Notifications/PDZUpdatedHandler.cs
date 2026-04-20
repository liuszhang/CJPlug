using Elsa.Studio.Contracts;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.ElsaIntegration.Notifications
{
    public class PDZUpdatedHandler : INotificationHandler<PDZUpdated>
    {
        public async Task HandleAsync(PDZUpdated notification, CancellationToken cancellationToken)
        {
            Log.Information("PDZUpdatedHandler");
        }
    }
}
