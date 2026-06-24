namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// 人员密级对数据密级的访问权限映射（多对多）
    /// PersonnelLevelItemId 和 DataLevelItemId 均作为 FK 指向 BasicEnumItem
    /// </summary>
    public class SecurityLevelAccess
    {
        public int Id { get; set; }

        /// <summary>人员密级项 ID（FK→BasicEnumItem，属于 personnel_secret_level 枚举）</summary>
        public int PersonnelLevelItemId { get; set; }

        /// <summary>数据密级项 ID（FK→BasicEnumItem，属于 data_secret_level 枚举）</summary>
        public int DataLevelItemId { get; set; }

        // 导航属性
        public BasicEnumItem PersonnelLevelItem { get; set; } = default!;
        public BasicEnumItem DataLevelItem { get; set; } = default!;
    }
}
