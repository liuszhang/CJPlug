using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using CJ.Plug.Models.DataFlow;
using CJ.Plug.Models.Shared;
using MudBlazor;
using Serilog.Parsing;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.SharedPages.DataConnector.Models
{
    public class PlugDataTableNodeModel: NodeModel
    {
        public PlugDataTableNodeModel(Point? position = null) : base(position) 
        {
            //PlugVariables = new List<PlugVariableItemModel>
            //{
            //    new PlugVariableItemModel
            //    {
            //        Name = "参数1",
            //        Type = "Integer"
            //    },
            //    new PlugVariableItemModel
            //    {
            //        Name = "参数2",
            //        Type = "Integer"
            //    }
            //};

            //AddPort(PlugVariables[0], PortAlignment.Left);
            //AddPort(PlugVariables[0], PortAlignment.Right);
            //AddPort(PlugVariables[1], PortAlignment.Left);
            //AddPort(PlugVariables[1], PortAlignment.Right);
        }
        public string? PlugName { get; set; }
        public string? PlugDefinitionId { get; set; }
        public List<PlugVariableItemModel>? PlugVariables { get; set; } = new List<PlugVariableItemModel>();
        public List<string>? ConnectInfo { get; set; } = new List<string>();

        public PlugVariablePort GetPort(PlugVariableItemModel variable, PortAlignment alignment) => Ports.Cast<PlugVariablePort>().FirstOrDefault(p => p.Variable == variable && p.Alignment == alignment);
        public PlugVariablePort? GetInPort(PlugVariableItemModel variable) => Ports.Cast<PlugVariablePort>().FirstOrDefault(p => p.Variable == variable && p.Alignment == PortAlignment.Left);
        public PlugVariablePort? GetOutPort(PlugVariableItemModel variable) => Ports.Cast<PlugVariablePort>().FirstOrDefault(p => p.Variable == variable && p.Alignment == PortAlignment.Right);
        public PlugVariablePort? GetPort(string identifier)=> Ports.Cast<PlugVariablePort>().FirstOrDefault(p => p.Identifier == identifier);

        public void AddPort(PlugVariableItemModel VariableItem)
        {
            //AddPort(new PlugVariablePort(this, column, alignment));
            AddInPort(VariableItem);
            AddOutPort(VariableItem);
        } 
        public void AddInPort(PlugVariableItemModel VariableItem) => AddPort(new PlugVariablePort(this, VariableItem, PortAlignment.Left,new PortIdentifierModel(this.PlugDefinitionId,VariableItem.Id, VariableItem.Name,"In").ToIdentifierString()));
        public void AddOutPort(PlugVariableItemModel VariableItem) => AddPort(new PlugVariablePort(this, VariableItem, PortAlignment.Right, new PortIdentifierModel(this.PlugDefinitionId, VariableItem.Id, VariableItem.Name, "Out").ToIdentifierString()));
    }
}
