namespace ToolMng.Contracts
{
    public interface IToolAgentNXServices
    {

        /// <summary>
        /// 获取NX模型参数表
        /// </summary>
        /// <param name="filePath">NX模型路径</param>
        /// <returns>NX参数表字符串</returns>
        Task<string?> GetNXParametersFromNXFileAsync(string filePath);

        /// <summary>
        /// 设置给定NX模型的参数值
        /// </summary>
        /// <param name="filePath">NX模型路径</param>
        /// <param name="newParameters">新的参数表值，标准格式是a=1,b=2,c=3</param>
        /// <returns>模型更新成功</returns>
        Task<bool> SetNXParametersToNXFileAsync(string filePath, string newParameters);
        /// <summary>
        /// 将NX模型导出为STL格式，用于轻量化展示
        /// </summary>
        /// <param name="NXFilePath">NX模型路径</param>
        /// <param name="StlFilePath">STL文件路径</param>
        /// <returns>STL文件路径，失败则为空</returns>
        Task<string?> ExportNXFileToStlFileAsync(string NXFilePath, string StlFilePath);

    }
}
