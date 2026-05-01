namespace ExcelPlug.Models;

public class ExcelPreviewData
{
    public string SheetName { get; set; } = "";
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
    public int TotalRows { get; set; }
    public int TotalColumns { get; set; }
    public bool IsTruncated { get; set; }
}
