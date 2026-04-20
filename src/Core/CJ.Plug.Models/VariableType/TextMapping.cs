using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.VariableType
{
    public class TextMapping
    {
        public int? Id { get; set; }

        //public string? InputName { get; set; }
        //public string? OutputName { get; set; }

        //无论是文本解析读还是写，统一使用绑定参数进行参数绑定，不再区分输入输出
        public string? BindingVariableName { get; set; }

        public string? Keywords { get; set; }
        public int[] Position { get; set; } = [1, 1, 1, 1];

        public int[] PositionOffset { get; set; } = [0, 0, 0, 0];
        public int StartOffset { get; set; } = 0; //替换区域起始位置相对于关键字起始位置的偏差量，用于以字符串形式更新文档
        public int EndOffset { get; set; } = 0;  //替换区域结束位置相对于关键字起始位置的偏差量，用于以字符串形式更新文档

        //标识识别模式，如键值对、关键字、表格等
        public ReadSchemaMode ReadSchemaMode { get; set; } = ReadSchemaMode.关键字;
        //读取数据的模式值，如关键字定位为[1,1,1,1]偏移数组，键值对为key:value的特定分割方式，表格为...
        public string? ReadSchemaValue { get; set; } //读取数据的schema值
    }


    public enum ReadSchemaMode
    {
        键值对,
        关键字,
        表格,
        //其他模式
    }
}
