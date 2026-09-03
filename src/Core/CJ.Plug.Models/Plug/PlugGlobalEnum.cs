using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Plug
{
    //注册所有的插头，用于其他地方使用，暂时未想到更好的处理方式
    public static class PlugGlobalEnum
    {
        public static NXGetParametersClass NXGetParameters => new NXGetParametersClass();
        public static NXSetParametersClass NXSetParameters => new NXSetParametersClass();
        public static NXToStlClass NXToStl => new NXToStlClass();
        public static CatiaGetParametersClass CatiaGetParameters => new CatiaGetParametersClass();
        public static CatiaSetParametersClass CatiaSetParameters => new CatiaSetParametersClass();
        public static CatiaExportStpClass CatiaExportStp => new CatiaExportStpClass();
        public static CatiaExportStlClass CatiaExportStl => new CatiaExportStlClass();
    }

    /// <summary>
    /// 共享NXGetParametersClass插头的配置信息
    /// </summary>
    public class NXGetParametersClass
    {

        public string TypeName = "NXGetParameters";
        public string ModelParametersKey = "ModelParameters";
        //public string InitVariables = "ModelFilePath";
        //public string ModelFilePath = "ModelFilePath";
        public VariablesList Variables;

        public class VariablesList
        {
            public string ModelFilePath = "ModelFilePath";
            public string ModelFileId = "ModelFileId";
        }


        public NXGetParametersClass()
        {
            Variables = new VariablesList();
        }
    }

    /// <summary>
    /// 更新NX模型参数工具类配置信息
    /// </summary>
    public class NXSetParametersClass
    {
        public string ToolName = "NXSetParameters";
        public string ToolVersion = "1.0";
        public string TypeName = "NXSetParameters";
        public string ModelParametersKey = "ModelParameters";
        public VariablesList Variables;

        public class VariablesList
        {
            public string ModelFilePath = "ModelFilePath";
            public string NewParameterString = "NewParameterString";
        }


        public NXSetParametersClass()
        {
            Variables = new VariablesList();
        }
    }


    public class NXToStlClass
    {

        public string TypeName = "NXToStl";
        public VariablesList Variables;

        public class VariablesList
        {
            public string ModelFilePath = "ModelFilePath";
            public string StlFilePath = "StlFilePath";
        }


        public NXToStlClass()
        {
            Variables = new VariablesList();
        }
    }

    /// <summary>
    /// 共享CatiaGetParametersClass插头的配置信息
    /// </summary>
    public class CatiaGetParametersClass
    {

        public string TypeName = "CatiaGetParameters";
        public string ModelParametersKey = "ModelParameters";
        public VariablesList Variables;

        public class VariablesList
        {
            public string ModelFilePath = "ModelFilePath";
            public string ModelFileId = "ModelFileId";
        }


        public CatiaGetParametersClass()
        {
            Variables = new VariablesList();
        }
    }

    /// <summary>
    /// 更新Catia模型参数工具类配置信息
    /// </summary>
    public class CatiaSetParametersClass
    {
        public string ToolName = "CatiaSetParameters";
        public string ToolVersion = "1.0";
        public string TypeName = "CatiaSetParameters";
        public string ModelParametersKey = "ModelParameters";
        public VariablesList Variables;

        public class VariablesList
        {
            public string ModelFilePath = "ModelFilePath";
            public string NewParameterString = "NewParameterString";
        }


        public CatiaSetParametersClass()
        {
            Variables = new VariablesList();
        }
    }

    /// <summary>
    /// Catia导出STP工具类配置信息
    /// </summary>
    public class CatiaExportStpClass
    {

        public string TypeName = "CatiaExportStp";
        public VariablesList Variables;

        public class VariablesList
        {
            public string ModelFilePath = "ModelFilePath";
            public string StpOutputPath = "StpOutputPath";
        }


        public CatiaExportStpClass()
        {
            Variables = new VariablesList();
        }
    }

    /// <summary>
    /// Catia导出STL工具类配置信息
    /// </summary>
    public class CatiaExportStlClass
    {

        public string TypeName = "CatiaExportStl";
        public VariablesList Variables;

        public class VariablesList
        {
            public string ModelFilePath = "ModelFilePath";
            public string StlOutputPath = "StlOutputPath";
            public string Sag = "Sag";
        }


        public CatiaExportStlClass()
        {
            Variables = new VariablesList();
        }
    }

    }
