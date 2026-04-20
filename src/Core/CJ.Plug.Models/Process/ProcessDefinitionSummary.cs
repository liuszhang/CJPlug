using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.PlugProcess
{
    public record ProcessDefinitionSummary(string Id, string DefinitionId, string Name, string? Description, int Version, bool IsLatest, bool IsPublished, string MaterializerName, DateTimeOffset CreatedAt);

}
