using Blazor.Diagrams.Core.Models;

namespace CJ.Plug.HomePage.Models.Landing.Groups;

public class ColoredGroupModel : GroupModel
{
    public ColoredGroupModel(IEnumerable<NodeModel> children, string color, byte padding = 30, bool autoSize = true) : base(children, padding, autoSize)
    {
        Color = color;
    }

    public string Color { get; }
}
