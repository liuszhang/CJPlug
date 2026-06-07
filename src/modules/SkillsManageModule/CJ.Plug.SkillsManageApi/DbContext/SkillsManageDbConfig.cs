using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Skills;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.SkillsManageApi;

public class SkillsManageDbConfig : IModuleDbConfig
{
    public void AddDbSets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Skill>(entity => entity.ToTable("Skills"));
    }

    public void ConfigEntities(ModelBuilder modelBuilder)
    {
    }
}