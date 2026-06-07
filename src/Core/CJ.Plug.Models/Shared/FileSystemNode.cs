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

        // ===== 新增字段（用于 FileTree 组件）=====
        /// <summary>
        /// 相对路径（用于树形展示）
        /// </summary>
        public string? RelativePath { get; set; }

        /// <summary>
        /// 文件类型（扩展名）
        /// </summary>
        public string? FileType { get; set; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get => Size; set => Size = value; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime? ModifiedTime { get => LastWriteTime; set => LastWriteTime = value; }

        /// <summary>
        /// 图标（MudBlazor Icons 字符串）
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// 图标颜色（MudBlazor Color）
        /// </summary>
        public string? IconColor { get; set; }

        /// <summary>
        /// 获取文件流的异步委托（用于下载）
        /// </summary>
        [JsonIgnore]
        public Func<Task<System.IO.Stream?>>? GetStreamAsync { get; set; }

        /// <summary>
        /// 删除文件的异步委托（返回是否成功）
        /// </summary>
        [JsonIgnore]
        public Func<Task<bool>>? DeleteAsync { get; set; }

        /// <summary>
        /// 删除文件时使用的路径（用于 MainApiClient.DeleteFileWithRequest 的 FilePath 参数）
        /// 当此属性有值时，FileTree 组件将直接使用 MainApiClient.DeleteFileWithRequest 进行删除
        /// </summary>
        [JsonIgnore]
        public string? DeletePath { get; set; }
    }
}
