using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Shared
{
    public class FileSystemNode
    {
        public FileSystemNode(string fullPath, string name, bool isDirectory = false)
        {
            FullPath = fullPath;
            Name = name;
            IsDirectory = isDirectory;
            Children = isDirectory ? new List<FileSystemNode>() : null;
        }

        public string? Name { get; set; }
        public string? FolderDisplayName { get; set; }
        public string? FullPath { get; set; }
        public bool IsDirectory { get; set; }
        public List<FileSystemNode>? Children { get; set; }
        
        // 文件元数据
        public long Size { get; set; }
        public string? Creator { get; set; }
        public DateTime? LastWriteTime { get; set; }
        
        [JsonIgnore]
        public HashSet<FileSystemNode>? HashChildren { get; set; }
    }
}
