using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Contracts
{
    public interface IModuleDbConfig
    {
        //注册DbSet
        void AddDbSets(ModelBuilder modelBuilder);
        //配置实体映射（包括TPH继承关系）
        void ConfigEntities(ModelBuilder modelBuilder);
    }
}
