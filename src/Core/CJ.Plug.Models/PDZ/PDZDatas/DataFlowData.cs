using CJ.Plug.Models.DataFlow;
using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


    public class DataFlowData : BaseVariable
    {
        [JsonIgnore]
        public PlugDataZone? PlugDataZone { get; set; }
        public int PlugDataZoneId { get; set; }

        public string? SourcePlugDefinitionId { get; set; }
        public string? SourceVariableName { get; set; }
        public int? SourceVariableId { get; set; }
        public string? TargetPlugDefinitionId { get; set; }
        public string? TargetVariableName { get; set; }
        public int? TargetVariableId { get; set; }

        //public PortLinkModel? PortLinkData { get; set; }
        public string? PortLinkData { get; set; }


        public string GeneratePortLinkData()
        {
            var PortLink = new PortLinkModel();
            PortLink.SourcePort = new PortIdentifierModel(SourcePlugDefinitionId, SourceVariableId, SourceVariableName, "Out");
            PortLink.TargetPort = new PortIdentifierModel(TargetPlugDefinitionId, TargetVariableId, TargetVariableName, "In");
            return JsonSerializer.Serialize(PortLink);
        }
    }


