namespace CJ.Plug.Models.Shared
{
    public class MoveDirectoryRequest
    {
        /// <summary>相对于 MainFileServerPathRoot 的源目录路径</summary>
        public string SourcePath { get; set; } = string.Empty;
        /// <summary>相对于 MainFileServerPathRoot 的目标目录路径</summary>
        public string DestPath { get; set; } = string.Empty;
    }
}
