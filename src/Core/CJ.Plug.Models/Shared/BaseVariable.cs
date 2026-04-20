using CJ.Plug.Models.VariableType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Shared
{
    public class BaseVariable
    {
        //
        // 摘要:
        //     The ID of the variable.
        public int? Id { get; set; }

        //当前对象的业务定义ID，注意它和PlugDefinitionId的区别，后者主要表达“属于哪个插头”
        //目前只是预留，还未有必须使用的场景
        public string? DefinitionId { get; set; }

        /// <summary>
        /// 父级元素定义ID
        /// </summary>
        //public string? ParentDefinitionId { get; set; }

        //
        // 摘要:
        //     The name of the variable.
        public string? Name { get; set; }

        //
        // 摘要:
        //     The DisplayName of the variable.
        public string? DisplayName { get; set; }

        //
        // 摘要:
        //     The Description of the variable.
        public string? Description { get; set; }

        //
        // 摘要:
        //     The Category of the variable.
        public string? Category { get; set; }

        //
        // 摘要:
        //     The UIHint of the variable.
        public string? UIHint { get; set; }

        //
        // 摘要:
        //     The StorageDriver of the variable.
        public string? StorageDriver { get; set; }

        //
        // 摘要:
        //     The type name of the variable.
        public string? Type { get; set; } = VariableTypeEnum.String.ToString();

        //
        // 摘要:
        //     Indicates whether the variable is an array.
        public bool IsArray { get; set; }=false;

        //
        // 摘要:
        //     The value of the variable.
        public string? Value { get; set; }

        /// <summary>
        /// 标示该参数值是否由其他插头参数传递而来
        /// </summary>
        public bool IsValueFromOtherVariable { get; set; } = false;
        /// <summary>
        /// 参数值来源插头的DefinitionId
        /// </summary>
        public string? SourceValuePlugDefinitionId { get; set; }
        /// <summary>
        /// 参数值来源参数的Id
        /// </summary>
        public int? SourceValueVariableId { get; set; }
        /// <summary>
        /// 参数值来源参数的名称，用于相关测试场景，因为可能测试时参数未正式保存，还未生成Id
        /// </summary>
        public string? SourceValueVariableName { get; set; }



        // 如果参数值来源于其他参数，记录原始参数的相关信息
        [Obsolete]
        public string? SourceValue { get; set; }


        public string? DefaultValue { get; set; }

        //用于展示的值，区别于Value是用于实际执行的值，比如引用参数DisplayValue为{123:test},Value是执行时实际的值，可能会变化，但不影响DisplayValue
        public string? DisplayValue { get; set; }

        //
        // 摘要:
        //     The storage driver type to use for persistence.
        public string? StorageDriverTypeName { get; set; }


        // 输入输出属性标志
        public string? InputAndOutput { get; set; }
        public bool IsInput { get; set; } = false;
        public bool IsOutput { get; set; } = false;
        public bool? IsBrowsable { get; set; } = true;//控制是否展示到参数表，配置类的参数设置为false
        public bool? IsRequired { get; set; } = false;

        // 是否初始化变量，如果是，则在插头初始化时，创建该变量并不允许删除
        public bool IsInitVariable { get; set; } = false;

        //
        // 摘要:
        //     The type name of the variable, including the array indicator.
        public string GetTypeDisplayName()
        {
            if (!IsArray)
            {
                return Type;
            }

            return Type + "[]";
        }
    }
}
