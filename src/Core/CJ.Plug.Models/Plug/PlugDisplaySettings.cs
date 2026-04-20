namespace CJ.Plug.Models.Plug;
/// <summary>
/// Represents display settings for an activity.
/// </summary>
public class PlugDisplaySettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlugDisplaySettings"/> class.
    /// </summary>
    public PlugDisplaySettings()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlugDisplaySettings"/> class.
    /// </summary>
    public PlugDisplaySettings(string color, string? icon = default)
    {
        Color = color;
        Icon = icon;
        DisplayName = null;
        ImgPath = null;
    }
    public PlugDisplaySettings(string color, string? icon, string? displayName)
    {
        Color = color;
        Icon = icon;
        DisplayName = displayName;
        ImgPath = null;
    }
    public PlugDisplaySettings(string color, string? icon, string? displayName, string? imgPath)
    {
        Color = color;
        Icon = icon;
        DisplayName = displayName;
        ImgPath = imgPath;
    }

    
    public string? Icon { get; set; }   
    public string? Color { get; set; } = default!;
    public string? DisplayName { get; set;} 
    public string? ImgPath { get; set; }
}