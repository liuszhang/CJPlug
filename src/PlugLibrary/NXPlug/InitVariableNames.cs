using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NXPlug
{
    /// <summary>
    /// NXPlug (主插头) 的初始化变量枚举
    /// </summary>
    public enum NXPlugVariables
    {
        /// <summary>NX 文件变量</summary>
        NXFile,
        /// <summary>STL 文件变量</summary>
        StlFile,
        /// <summary>模型参数变量</summary>
        ModelParameters,
    }
    
    /// <summary>
    /// NXGetParameters (获取参数) 的初始化变量枚举
    /// </summary>
    public enum NXGetParametersVariables
    {
        /// <summary>模型文件路径</summary>
        ModelFilePath,
        /// <summary>结果字符串</summary>
        ResultString
    }
    
    /// <summary>
    /// NXSetParameters (设置参数) 的初始化变量枚举
    /// </summary>
    public enum NXSetParametersVariables
    {
        /// <summary>模型文件路径</summary>
        ModelFilePath,
        /// <summary>新参数字符串</summary>
        NewParameterString,
        /// <summary>结果字符串</summary>
        ResultString
    }
    
    /// <summary>
    /// NXToStl (模型转STL) 的初始化变量枚举
    /// </summary>
    public enum NXToStlVariables
    {
        /// <summary>NX 部件文件（.prt）路径</summary>
        PrtFilePath,
        /// <summary>STL 输出文件路径</summary>
        StlOutputPath,
        /// <summary>弦高公差（Chordal Tolerance）</summary>
        ChordalTol,
        /// <summary>邻接公差（Adjacency Tolerance）</summary>
        AdjacencyTol,
        /// <summary>是否自动生成法线</summary>
        AutoNormalGen,
        /// <summary>执行结果字符串</summary>
        ResultString
    }
}
