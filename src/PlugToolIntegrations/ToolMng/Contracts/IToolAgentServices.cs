namespace ToolMng.Contracts
{
    public interface IToolAgentServices
    {
        /// <summary>
        /// 上传文件至图站，用于后续图站工具处理
        /// </summary>
        /// <param name="file"></param>
        /// <param name="index">图站文件区分索引，暂定为插头插入ID</param>
        /// <returns>图站上传后的真实路径</returns>
        Task<string> UploadFileToToolAgentFileServerAsync(IFormFile file,string index);

        /// <summary>
        /// 上传文件至图站，用于后续图站工具处理
        /// </summary>
        /// <param name="file"></param>
        /// <returns>图站上传后的真实路径</returns>
        Task<string?> UploadFileToToolAgentFileServerAsync(IFormFile file);
        /// <summary>
        /// 数据流方式上传文件至图站，用于后续图站工具处理
        /// </summary>
        /// <param name="file"></param>
        /// <returns>图站上传后的真实路径</returns>
        Task<string?> UploadFileStreamToToolAgentFileServerAsync(Stream file,string fileName);


        Task DeleteFile(string filePath);
    }
}
