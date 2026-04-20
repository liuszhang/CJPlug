public partial class PlugDataZone
    {        
        //获取PDZ中插头数据列表(不含自己)
        public List<PlugData> GetPlugDefinitonIdsInPDZ()
        {
            return PlugDatas?
                .Where(p=>p.PlugDefinitionId!=PlugDefinitionId) //不找流程自己的数据
                .ToList() ?? new List<PlugData>();
        }

        public PlugData? GetPlugData(string PlugDefinitionId)
        {
            return PlugDatas?
                .Where(p => p.PlugDefinitionId == PlugDefinitionId)
                .FirstOrDefault();
        }

        //设置PDZ中的插头数据，主要是用于更新数据流中的位置信息
        public PlugData? SetPlugPosition(string? PlugDefinitionId, string? value)
        {
            var PlugData = PlugDatas?
                .Where(p => p.PlugDefinitionId == PlugDefinitionId)
                .FirstOrDefault();
            if (PlugData != null)
            {
                PlugData.PlugPosition = value;
                return PlugData;
            }
            else
            {
                return null;
            }
        }




    }

