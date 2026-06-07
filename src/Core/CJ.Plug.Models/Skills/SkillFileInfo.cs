namespace CJ.Plug.Models.Skills
{
    /// <summary>
    /// 技能关联文件信息模型
    /// </summary>
    public class SkillFileInfo
    {
        /// <summary>文件名</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>文件相对路径</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>文件大小（字节）</summary>
        public long FileSize { get; set; }

        /// <summary>文件类型（扩展名）</summary>
        public string FileType { get; set; } = string.Empty;

        /// <summary>最后修改时间</summary>
        public DateTime ModifiedTime { get; set; }
    }
}