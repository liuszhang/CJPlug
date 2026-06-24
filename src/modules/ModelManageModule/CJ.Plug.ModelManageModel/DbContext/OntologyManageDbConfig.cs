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
            modelBuilder.Entity<SecurityLevelAccess>(entity => entity.ToTable("SecurityLevelAccesses"));
            modelBuilder.Entity<PropertyConstraint>(entity => entity.ToTable("PropertyConstraints"));

            // M4 场景
            modelBuilder.Entity<Scenario>(entity => entity.ToTable("Scenarios"));

            // M5 主体 & 权限
            modelBuilder.Entity<Subject>(entity => entity.ToTable("Subjects"));
            modelBuilder.Entity<Permission>(entity => entity.ToTable("Permissions"));

            // M5.5 接口契约
            modelBuilder.Entity<ExternalSystem>(entity => entity.ToTable("ExternalSystems"));
            modelBuilder.Entity<InterfaceContract>(entity => entity.ToTable("InterfaceContracts"));

            // M6 可靠性
            modelBuilder.Entity<ExceptionType>(entity => entity.ToTable("ExceptionTypes"));
            modelBuilder.Entity<CompensationAction>(entity => entity.ToTable("CompensationActions"));

            // M7 质量
            modelBuilder.Entity<QualityMetric>(entity => entity.ToTable("QualityMetrics"));
            modelBuilder.Entity<AlertRule>(entity => entity.ToTable("AlertRules"));
            modelBuilder.Entity<ImprovementAction>(entity => entity.ToTable("ImprovementActions"));

            Console.WriteLine("------>Success Add OntologyManage Module DbSet Config");
        }

        public void ConfigEntities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Ontology>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasOne(e => e.Parent)
                      .WithMany(e => e.Children)
                      .HasForeignKey(e => e.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);
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

            // ── 密级访问权限 ──
            modelBuilder.Entity<SecurityLevelAccess>(entity =>
            {
                entity.HasIndex(e => new { e.PersonnelLevelItemId, e.DataLevelItemId }).IsUnique();
                entity.HasOne(e => e.PersonnelLevelItem)
                      .WithMany()
                      .HasForeignKey(e => e.PersonnelLevelItemId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.DataLevelItem)
                      .WithMany()
                      .HasForeignKey(e => e.DataLevelItemId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ── M4 场景 ──
            modelBuilder.Entity<Scenario>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.OntologyId);
            });

            // ── M5 主体 & 权限 ──
            modelBuilder.Entity<Subject>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasOne(e => e.Parent)
                      .WithMany(e => e.Children)
                      .HasForeignKey(e => e.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.SubjectId);
                entity.HasOne(e => e.Subject)
                      .WithMany(e => e.Permissions)
                      .HasForeignKey(e => e.SubjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── M5.5 接口契约 ──
            modelBuilder.Entity<ExternalSystem>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
            });

            modelBuilder.Entity<InterfaceContract>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.ExternalSystemId);
                entity.HasOne(e => e.ExternalSystem)
                      .WithMany(e => e.InterfaceContracts)
                      .HasForeignKey(e => e.ExternalSystemId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ── M6 可靠性 ──
            modelBuilder.Entity<ExceptionType>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
            });

            modelBuilder.Entity<CompensationAction>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.ExceptionTypeId);
                entity.HasOne(e => e.ExceptionType)
                      .WithMany(e => e.CompensationActions)
                      .HasForeignKey(e => e.ExceptionTypeId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ── M7 质量 ──
            modelBuilder.Entity<QualityMetric>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.OntologyId);
            });

            modelBuilder.Entity<AlertRule>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.QualityMetricId);
                entity.HasOne(e => e.QualityMetric)
                      .WithMany(e => e.AlertRules)
                      .HasForeignKey(e => e.QualityMetricId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ImprovementAction>(entity =>
            {
                entity.HasIndex(e => e.Code).IsUnique();
                entity.HasIndex(e => e.QualityMetricId);
                entity.HasOne(e => e.QualityMetric)
                      .WithMany(e => e.ImprovementActions)
                      .HasForeignKey(e => e.QualityMetricId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            Console.WriteLine("------>Success Add OntologyManage Module Db EntityConfig");
        }
    }
}
