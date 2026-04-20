using Blazor.Diagrams.Core.Models;

namespace CJ.Plug.HomePage.Models.Nodes;

public class ArithmeticContainer : GroupModel
{
    public ArithmeticContainer(IEnumerable<NodeModel> children, byte padding = 30, bool autoSize = true) : base(children, padding, autoSize)
    {
    }
}
