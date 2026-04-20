using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public class PDZFilter
    {
        public string? PDZId { get; set; } //UserName + ProcessDefinitionId;//ProcessJobInstanceId
        public string? Type { get; set; } = PDZTypeEnum.DesignPDZ.ToString();
        public string? JobDefinitionId { get; set; }
        public string? PlugDefinitionId { get; set; }
        public string? UserName { get; set; }
        public string? WorkPath { get; set; }
    }

