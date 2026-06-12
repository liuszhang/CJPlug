namespace CJ.Plug.Desktop.Models;

public class BreadcrumbItem
{
    public string Text { get; set; } = string.Empty;
    public string? NavigateUrl { get; set; }
    public bool IsFirst { get; set; }
    public bool IsClickable => NavigateUrl != null;
}