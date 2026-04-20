namespace ToolMng.Models
{
    public class FileSystemItem
    {
        public string Name { get; set; }
        public string? FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public List<FileSystemItem> Children { get; set; }

        public FileSystemItem(string fullPath, string name, bool isDirectory = false)
        {
            FullPath = fullPath;
            Name = name;
            IsDirectory = isDirectory;
            Children = isDirectory ? new List<FileSystemItem>() : null;
        }
    }
}
