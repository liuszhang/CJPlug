using CJ.Plug.ModelManageModel.Models;

namespace CJ.Plug.ModelManageApi.Models;

public class CreateBehaviorRequest
{
    public ObjectBehavior Behavior { get; set; } = new();
    public List<int>? RuleIds { get; set; }
}