using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public enum PDZTypeEnum
    {
        DesignPDZ,
        ExecutePDZ,
        JobPDZ,
        Desi,  //设计PDZ，流程中单节点执行时使用
        Job1,  //通过编辑器执行的Job PDZ
        Job2,  //通过执行页面执行的Job PDZ
        Job3,  //通过共享流程执行的Job PDZ

    }

