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

    }
