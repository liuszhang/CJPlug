using CJ.Plug.ModelManageModel.Models;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.ModelManageModel.DbContext
{
    public class OntologyManageDbConfig : IModuleDbConfig
    {
        public void AddDbSets(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Ontology>(entity => entity.ToTable(DbTableNameEnum.Ontologies.ToString()));
            modelBuilder.Entity<Property>(entity => entity.ToTable("Properties"));
            modelBuilder.Entity<ObjectInstance>(entity => entity.ToTable("ObjectInstances"));
            modelBuilder.Entity<OntologyRelationship>(entity => entity.ToTable("OntologyRelationships"));
            modelBuilder.Entity<ObjectBehavior>(entity => entity.ToTable("ObjectBehaviors"));
            modelBuilder.Entity<OntologyRule>(entity => entity.ToTable("OntologyRules"));
            modelBuilder.Entity<BasicEnum>(entity => entity.ToTable("BasicEnums"));
            modelBuilder.Entity<BasicEnumItem>(entity => entity.ToTable("BasicEnumItems"));
            modelBuilder.Entity<PropertyConstraint>(entity => entity.ToTable("PropertyConstraints"));

            Console.WriteLine("------>Success Add OntologyManage Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Ontology>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasMany(e => e.Properties)
                      .WithOne()
                      .HasForeignKey(f => f.OntologyId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasMany(e => e.OutgoingRelationships)
                      .WithOne()
                      .HasForeignKey(r => r.SourceOntologyId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(e => e.IncomingRelationships)
                      .WithOne()
                      .HasForeignKey(r => r.TargetOntologyId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(e => e.Behaviors)
                      .WithOne()
                      .HasForeignKey(b => b.OntologyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Property>(entity =>
            {
                entity.HasIndex(e => new { e.OntologyId, e.Code }).IsUnique();
                entity.HasMany(e => e.Constraints)
                      .WithOne(c => c.Property)
                      .HasForeignKey(c => c.PropertyId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ObjectInstance>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.OntologyId);
                entity.HasIndex(e => e.CreatedAt);
            });

            modelBuilder.Entity<OntologyRelationship>(entity =>
            {
                entity.HasIndex(e => new { e.SourceOntologyId, e.Code }).IsUnique();
                entity.HasIndex(e => e.TargetOntologyId);
            });

            modelBuilder.Entity<ObjectBehavior>(entity =>
            {
                entity.HasIndex(e => e.OntologyId);
            });

            modelBuilder.Entity<OntologyRule>(entity =>
            {
                // 规则通过 CommonRelation 模块管理关联关系，不再直接持有外键
            });

            modelBuilder.Entity<BasicEnum>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasMany(e => e.Items)
                      .WithOne()
                      .HasForeignKey(f => f.EnumId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BasicEnumItem>(entity =>
            {
                entity.HasIndex(e => new { e.EnumId, e.Code }).IsUnique();
            });

            Console.WriteLine("------>Success Add OntologyManage Module Db EntityConfig");
        }
    }
}
