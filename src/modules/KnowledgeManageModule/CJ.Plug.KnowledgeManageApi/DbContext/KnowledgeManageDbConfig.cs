using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Knowledge;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.KnowledgeManageApi;

/// <summary>
/// 知识管理模块数据库配置：注册 DbSet 和实体映射
/// </summary>
public class KnowledgeManageDbConfig : IModuleDbConfig
{
    /// <summary>
    /// 注册 KnowledgeBases、KnowledgeFolders 和 KnowledgeItems DbSet
    /// </summary>
    public void AddDbSets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnowledgeBase>(entity => entity.ToTable("KnowledgeBases"));
        modelBuilder.Entity<KnowledgeFolder>(entity => entity.ToTable("KnowledgeFolders"));
        modelBuilder.Entity<KnowledgeItem>(entity => entity.ToTable("KnowledgeItems"));
    }

    /// <summary>
    /// 配置实体映射：自关联外键和级联删除策略
    /// </summary>
    public void ConfigEntities(ModelBuilder modelBuilder)
    {
        // KnowledgeBase → KnowledgeFolder：级联删除
        modelBuilder.Entity<KnowledgeBase>(entity =>
        {
            entity.HasMany(b => b.Folders)
                .WithOne(f => f.Base)
                .HasForeignKey(f => f.BaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // KnowledgeFolder 自关联：ParentId → KnowledgeFolder.Id，OnDelete Restrict
        modelBuilder.Entity<KnowledgeFolder>(entity =>
        {
            entity.HasOne(f => f.Parent)
                .WithMany(f => f.Children)
                .HasForeignKey(f => f.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(f => f.Items)
                .WithOne(i => i.Folder)
                .HasForeignKey(i => i.FolderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
