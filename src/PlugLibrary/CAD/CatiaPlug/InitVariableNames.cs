using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatiaPlug
{
    /// <summary>
    /// CatiaPlug (主插头) 的初始化变量枚举
    /// </summary>
    public enum CatiaPlugVariables
    {
        /// <summary>Catia 模型文件变量</summary>
        CatiaFile,
        /// <summary>STL 文件变量</summary>
        StlFile,
        /// <summary>模型参数变量</summary>
        ModelParameters,
    }

    /// <summary>
    /// CatiaGetParameters (获取参数) 的初始化变量枚举
    /// </summary>
    public enum CatiaGetParametersVariables
    {
        /// <summary>模型文件路径</summary>
        ModelFilePath,
        /// <summary>结果字符串</summary>
        ResultString
    }

    /// <summary>
    /// CatiaSetParameters (设置参数) 的初始化变量枚举
    /// </summary>
    public enum CatiaSetParametersVariables
    {
        /// <summary>模型文件路径</summary>
        ModelFilePath,
        /// <summary>新参数字符串</summary>
        NewParameterString,
        /// <summary>结果字符串</summary>
        ResultString
    }

    /// <summary>
    /// CatiaExportStp (导出STP) 的初始化变量枚举
    /// </summary>
    public enum CatiaExportStpVariables
    {
        /// <summary>模型文件路径</summary>
        ModelFilePath,
        /// <summary>STP 输出文件路径</summary>
        StpOutputPath,
        /// <summary>结果字符串</summary>
        ResultString
    }

    /// <summary>
    /// CatiaExportStl (导出STL) 的初始化变量枚举
    /// </summary>
    public enum CatiaExportStlVariables
    {
        /// <summary>模型文件路径</summary>
        ModelFilePath,
        /// <summary>STL 输出文件路径</summary>
        StlOutputPath,
        /// <summary>弦高偏差（Sag）</summary>
        Sag,
        /// <summary>结果字符串</summary>
        ResultString
    }
}
