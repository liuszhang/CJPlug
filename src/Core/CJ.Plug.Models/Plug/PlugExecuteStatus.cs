namespace CJ.Plug.Models.Plug;
/// <summary>
/// Represents a report of activity executions.
/// </summary>
public class PlugExecuteStatus
{
    public int? Id { get; set; }
    public string? Status { get; set; }
    public string? SubStatus { get; set; }


    /// <summary>
    /// The number of times the activity has been started.
    /// </summary>
    public long Started { get; set; }
    
    /// <summary>
    /// The number of times the activity has been completed.
    /// </summary>
    public long Completed { get; set; }
    
    /// <summary>
    /// The number of times the activity has been uncompleted.
    /// </summary>
    public long Uncompleted { get; set; }
    
    /// <summary>
    /// Whether the activity is blocked.
    /// </summary>
    public bool Blocked { get; set; }
    
    /// <summary>
    /// Whether the activity has faulted.
    /// </summary>
    public bool Faulted { get; set; }
}