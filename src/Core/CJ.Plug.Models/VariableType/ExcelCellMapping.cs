namespace CJ.Plug.Models.VariableType
{
    public class ExcelCellMapping
    {
        public int? Id { get; set; }
        public string? CellRef { get; set; }
        public string? Value { get; set; }
        public bool? NeedEval { get; set; }

        /// <summary>是否绑定到其他插头的参数变量</summary>
        public bool? IsBoundToVariable { get; set; }

        /// <summary>来源变量名称</summary>
        public string? BoundVariableName { get; set; }

        /// <summary>来源插头 DefinitionId</summary>
        public string? BoundPlugDefinitionId { get; set; }
    }
}
