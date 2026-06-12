using CJ.Plug.ModelManageApi.Contracts;
using CJ.Plug.ModelManageModel.Models;
using CJ.Plug.Models.Relation;
using CJ.Plug.Models.Shared;
using Microsoft.EntityFrameworkCore;

namespace CJ.Plug.ModelManageApi.Services
{
    public class OntologyManageService : IOntologyManageService
    {
        private readonly MainDbContext _dbContext;

        public OntologyManageService(MainDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ========== 本体 CRUD ==========
        public async Task<IEnumerable<Ontology>> GetAllOntologiesAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<Ontology>()
                .Include(o => o.Properties)
                .ToListAsync(ct);
        }

        public async Task<Ontology?> GetOntologyByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<Ontology>()
                .Include(o => o.Properties.OrderBy(p => p.SortOrder))
                .Include(o => o.OutgoingRelationships)
                .Include(o => o.IncomingRelationships)
                .Include(o => o.Behaviors)
                .FirstOrDefaultAsync(o => o.Id == id, ct);
        }

        public async Task<Ontology?> CreateOntologyAsync(Ontology ontology, CancellationToken ct = default)
        {
            ontology.CreatedAt = DateTime.UtcNow.ToLocalTime();
            ontology.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<Ontology>().Add(ontology);
            await _dbContext.SaveChangesAsync(ct);
            return ontology;
        }

        public async Task<Ontology?> UpdateOntologyAsync(Ontology ontology, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<Ontology>().FirstOrDefaultAsync(o => o.Id == ontology.Id, ct);
            if (existing == null) return null;

            existing.Name = ontology.Name;
            existing.DisplayName = ontology.DisplayName;
            existing.Description = ontology.Description;
            existing.Version = ontology.Version;
            existing.Category = ontology.Category;
            existing.IsSystem = ontology.IsSystem;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteOntologyAsync(int id, CancellationToken ct = default)
        {
            var ontology = await _dbContext.Set<Ontology>().FirstOrDefaultAsync(o => o.Id == id, ct);
            if (ontology == null) return false;
            if (ontology.IsSystem) return false;
            _dbContext.Set<Ontology>().Remove(ontology);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== 属性 CRUD ==========
        public async Task<IEnumerable<Property>> GetPropertiesByOntologyIdAsync(int ontologyId, CancellationToken ct = default)
        {
            return await _dbContext.Set<Property>()
                .Where(p => p.OntologyId == ontologyId)
                .OrderBy(p => p.SortOrder)
                .ToListAsync(ct);
        }

        public async Task<Property?> GetPropertyByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<Property>().FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<Property?> CreatePropertyAsync(Property property, CancellationToken ct = default)
        {
            property.CreatedAt = DateTime.UtcNow.ToLocalTime();
            property.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<Property>().Add(property);
            await _dbContext.SaveChangesAsync(ct);
            return property;
        }

        public async Task<Property?> UpdatePropertyAsync(Property property, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<Property>().FirstOrDefaultAsync(p => p.Id == property.Id, ct);
            if (existing == null) return null;

            existing.Name = property.Name;
            existing.DisplayName = property.DisplayName;
            existing.Description = property.Description;
            existing.PropertyType = property.PropertyType;
            existing.Value = property.Value;
            existing.IsRequired = property.IsRequired;
            existing.SortOrder = property.SortOrder;
            existing.UIHint = property.UIHint;
            existing.SelectOptions = property.SelectOptions;
            existing.ValidationRule = property.ValidationRule;
            existing.IsBrowsable = property.IsBrowsable;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeletePropertyAsync(int id, CancellationToken ct = default)
        {
            var property = await _dbContext.Set<Property>().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (property == null) return false;
            _dbContext.Set<Property>().Remove(property);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ReorderPropertiesAsync(int ontologyId, List<int> propertyIds, CancellationToken ct = default)
        {
            var properties = await _dbContext.Set<Property>()
                .Where(p => p.OntologyId == ontologyId)
                .ToListAsync(ct);
            for (int i = 0; i < propertyIds.Count; i++)
            {
                var prop = properties.FirstOrDefault(p => p.Id == propertyIds[i]);
                if (prop != null)
                {
                    prop.SortOrder = i;
                    prop.UpdatedAt = DateTime.UtcNow.ToLocalTime();
                }
            }
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== 关系 CRUD ==========
        public async Task<IEnumerable<OntologyRelationship>> GetRelationshipsByOntologyIdAsync(int ontologyId, CancellationToken ct = default)
        {
            return await _dbContext.Set<OntologyRelationship>()
                .Where(r => r.SourceOntologyId == ontologyId || r.TargetOntologyId == ontologyId)
                .OrderBy(r => r.SortOrder)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<OntologyRelationship>> GetAllRelationshipsAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<OntologyRelationship>()
                .OrderBy(r => r.SortOrder)
                .ToListAsync(ct);
        }

        public async Task<OntologyRelationship?> GetRelationshipByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<OntologyRelationship>().FirstOrDefaultAsync(r => r.Id == id, ct);
        }

        public async Task<OntologyRelationship?> CreateRelationshipAsync(OntologyRelationship relationship, CancellationToken ct = default)
        {
            relationship.CreatedAt = DateTime.UtcNow.ToLocalTime();
            relationship.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<OntologyRelationship>().Add(relationship);
            await _dbContext.SaveChangesAsync(ct);
            return relationship;
        }

        public async Task<OntologyRelationship?> UpdateRelationshipAsync(OntologyRelationship relationship, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<OntologyRelationship>().FirstOrDefaultAsync(r => r.Id == relationship.Id, ct);
            if (existing == null) return null;

            existing.Name = relationship.Name;
            existing.DisplayName = relationship.DisplayName;
            existing.Description = relationship.Description;
            existing.TargetOntologyId = relationship.TargetOntologyId;
            existing.Cardinality = relationship.Cardinality;
            existing.RelationshipType = relationship.RelationshipType;
            existing.InverseName = relationship.InverseName;
            existing.IsRequired = relationship.IsRequired;
            existing.SortOrder = relationship.SortOrder;
            existing.IsEnabled = relationship.IsEnabled;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteRelationshipAsync(int id, CancellationToken ct = default)
        {
            var relationship = await _dbContext.Set<OntologyRelationship>().FirstOrDefaultAsync(r => r.Id == id, ct);
            if (relationship == null) return false;
            _dbContext.Set<OntologyRelationship>().Remove(relationship);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== 行为 CRUD ==========
        public async Task<IEnumerable<ObjectBehavior>> GetBehaviorsByOntologyIdAsync(int ontologyId, CancellationToken ct = default)
        {
            return await _dbContext.Set<ObjectBehavior>()
                .Where(b => b.OntologyId == ontologyId)
                .OrderBy(b => b.SortOrder)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<ObjectBehavior>> GetAllBehaviorsAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<ObjectBehavior>()
                .OrderBy(b => b.SortOrder)
                .ToListAsync(ct);
        }

        public async Task<ObjectBehavior?> GetBehaviorByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<ObjectBehavior>().FirstOrDefaultAsync(b => b.Id == id, ct);
        }

        public async Task<ObjectBehavior?> CreateBehaviorAsync(ObjectBehavior behavior, List<int>? ruleIds = null, CancellationToken ct = default)
        {
            behavior.CreatedAt = DateTime.UtcNow.ToLocalTime();
            behavior.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<ObjectBehavior>().Add(behavior);
            await _dbContext.SaveChangesAsync(ct);

            // 写入 CommonRelation 记录（RuleToBehavior）
            if (ruleIds?.Any() == true)
            {
                foreach (var ruleId in ruleIds.Distinct())
                {
                    var relation = new CommonRelation
                    {
                        RelationCategory = RelationCategory.RuleToBehavior.ToString(),
                        RoleAId = ruleId,
                        RoleBId = behavior.Id,
                        RoleAName = $"Rule:{ruleId}",
                        RoleBName = $"Behavior:{behavior.Id}",
                        RelationType = RelationTypes.应用.ToString()
                    };
                    _dbContext.Set<CommonRelation>().Add(relation);
                }
                await _dbContext.SaveChangesAsync(ct);
            }

            return behavior;
        }

        public async Task<ObjectBehavior?> UpdateBehaviorAsync(ObjectBehavior behavior, List<int>? ruleIds = null, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<ObjectBehavior>().FirstOrDefaultAsync(b => b.Id == behavior.Id, ct);
            if (existing == null) return null;

            existing.Name = behavior.Name;
            existing.Description = behavior.Description;
            existing.IsEnabled = behavior.IsEnabled;
            existing.SortOrder = behavior.SortOrder;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);

            // 同步关联规则：先删后加
            if (ruleIds != null)
            {
                var oldRelations = await _dbContext.Set<CommonRelation>()
                    .Where(r => r.RelationCategory == RelationCategory.RuleToBehavior.ToString()
                             && r.RoleBId == behavior.Id)
                    .ToListAsync(ct);
                _dbContext.Set<CommonRelation>().RemoveRange(oldRelations);

                foreach (var ruleId in ruleIds.Distinct())
                {
                    var relation = new CommonRelation
                    {
                        RelationCategory = RelationCategory.RuleToBehavior.ToString(),
                        RoleAId = ruleId,
                        RoleBId = behavior.Id,
                        RoleAName = $"Rule:{ruleId}",
                        RoleBName = $"Behavior:{behavior.Id}",
                        RelationType = RelationTypes.应用.ToString()
                    };
                    _dbContext.Set<CommonRelation>().Add(relation);
                }
                await _dbContext.SaveChangesAsync(ct);
            }

            return existing;
        }

        public async Task<bool> DeleteBehaviorAsync(int id, CancellationToken ct = default)
        {
            var behavior = await _dbContext.Set<ObjectBehavior>().FirstOrDefaultAsync(b => b.Id == id, ct);
            if (behavior == null) return false;
            _dbContext.Set<ObjectBehavior>().Remove(behavior);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IEnumerable<OntologyRule>> GetRulesByBehaviorIdAsync(int behaviorId, CancellationToken ct = default)
        {
            var relatedRuleIds = await _dbContext.Set<CommonRelation>()
                .Where(r => r.RelationCategory == RelationCategory.RuleToBehavior.ToString()
                         && r.RoleBId == behaviorId)
                .Select(r => r.RoleAId)
                .ToListAsync(ct);

            if (!relatedRuleIds.Any())
                return Enumerable.Empty<OntologyRule>();

            return await _dbContext.Set<OntologyRule>()
                .Where(r => relatedRuleIds.Contains(r.Id))
                .OrderBy(r => r.SortOrder)
                .ToListAsync(ct);
        }

        // ========== 规则 CRUD ==========
        public async Task<IEnumerable<OntologyRule>> GetRulesByOntologyIdAsync(int ontologyId, CancellationToken ct = default)
        {
            // 通过 CommonRelation 查找规则与本体关联
            var relatedRuleIds = await _dbContext.Set<CommonRelation>()
                .Where(r => r.RelationCategory == RelationCategory.RuleToOntology.ToString()
                         && r.RoleBId == ontologyId)
                .Select(r => r.RoleAId)
                .ToListAsync(ct);

            if (!relatedRuleIds.Any())
                return Enumerable.Empty<OntologyRule>();

            return await _dbContext.Set<OntologyRule>()
                .Where(r => relatedRuleIds.Contains(r.Id))
                .OrderBy(r => r.SortOrder)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<OntologyRule>> GetAllRulesAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<OntologyRule>()
                .OrderBy(r => r.SortOrder)
                .ToListAsync(ct);
        }

        public async Task<OntologyRule?> GetRuleByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<OntologyRule>().FirstOrDefaultAsync(r => r.Id == id, ct);
        }

        public async Task<OntologyRule?> CreateRuleAsync(OntologyRule rule, int? associatedOntologyId = null, int? associatedBehaviorId = null, CancellationToken ct = default)
        {
            rule.CreatedAt = DateTime.UtcNow.ToLocalTime();
            rule.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<OntologyRule>().Add(rule);
            await _dbContext.SaveChangesAsync(ct);

            // 通过 CommonRelation 模块保存关联关系
            if (associatedOntologyId.HasValue)
            {
                var relation = new CommonRelation
                {
                    RelationCategory = RelationCategory.RuleToOntology.ToString(),
                    RoleAId = rule.Id,
                    RoleBId = associatedOntologyId.Value,
                    RoleAName = $"Rule:{rule.Id}",
                    RoleBName = $"Ontology:{associatedOntologyId.Value}",
                    RelationType = RelationTypes.应用.ToString()
                };
                _dbContext.Set<CommonRelation>().Add(relation);
            }
            if (associatedBehaviorId.HasValue)
            {
                var relation = new CommonRelation
                {
                    RelationCategory = RelationCategory.RuleToBehavior.ToString(),
                    RoleAId = rule.Id,
                    RoleBId = associatedBehaviorId.Value,
                    RoleAName = $"Rule:{rule.Id}",
                    RoleBName = $"Behavior:{associatedBehaviorId.Value}",
                    RelationType = RelationTypes.应用.ToString()
                };
                _dbContext.Set<CommonRelation>().Add(relation);
            }
            if (associatedOntologyId.HasValue || associatedBehaviorId.HasValue)
            {
                await _dbContext.SaveChangesAsync(ct);
            }

            return rule;
        }

        public async Task<OntologyRule?> UpdateRuleAsync(OntologyRule rule, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<OntologyRule>().FirstOrDefaultAsync(r => r.Id == rule.Id, ct);
            if (existing == null) return null;

            existing.Name = rule.Name;
            existing.Description = rule.Description;
            existing.IsEnabled = rule.IsEnabled;
            existing.EffectiveFrom = rule.EffectiveFrom;
            existing.EffectiveTo = rule.EffectiveTo;
            existing.SortOrder = rule.SortOrder;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteRuleAsync(int id, CancellationToken ct = default)
        {
            var rule = await _dbContext.Set<OntologyRule>().FirstOrDefaultAsync(r => r.Id == id, ct);
            if (rule == null) return false;
            _dbContext.Set<OntologyRule>().Remove(rule);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== 基础枚举 CRUD ==========
        public async Task<IEnumerable<BasicEnum>> GetAllBasicEnumsAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<BasicEnum>()
                .Include(e => e.Items.OrderBy(i => i.SortOrder))
                .OrderBy(e => e.SortOrder)
                .ToListAsync(ct);
        }

        public async Task<BasicEnum?> GetBasicEnumByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<BasicEnum>()
                .Include(e => e.Items.OrderBy(i => i.SortOrder))
                .FirstOrDefaultAsync(e => e.Id == id, ct);
        }

        public async Task<BasicEnum?> CreateBasicEnumAsync(BasicEnum basicEnum, CancellationToken ct = default)
        {
            basicEnum.CreatedAt = DateTime.UtcNow.ToLocalTime();
            basicEnum.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<BasicEnum>().Add(basicEnum);
            await _dbContext.SaveChangesAsync(ct);
            return basicEnum;
        }

        public async Task<BasicEnum?> UpdateBasicEnumAsync(BasicEnum basicEnum, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<BasicEnum>().FirstOrDefaultAsync(e => e.Id == basicEnum.Id, ct);
            if (existing == null) return null;

            existing.Name = basicEnum.Name;
            existing.DisplayName = basicEnum.DisplayName;
            existing.Description = basicEnum.Description;
            existing.SortOrder = basicEnum.SortOrder;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteBasicEnumAsync(int id, CancellationToken ct = default)
        {
            var basicEnum = await _dbContext.Set<BasicEnum>().FirstOrDefaultAsync(e => e.Id == id, ct);
            if (basicEnum == null) return false;
            if (basicEnum.IsSystem) return false;
            _dbContext.Set<BasicEnum>().Remove(basicEnum);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== 基础枚举项 CRUD ==========
        public async Task<IEnumerable<BasicEnumItem>> GetEnumItemsAsync(int enumId, CancellationToken ct = default)
        {
            return await _dbContext.Set<BasicEnumItem>()
                .Where(i => i.EnumId == enumId)
                .OrderBy(i => i.SortOrder)
                .ToListAsync(ct);
        }

        public async Task<BasicEnumItem?> CreateEnumItemAsync(BasicEnumItem item, CancellationToken ct = default)
        {
            item.CreatedAt = DateTime.UtcNow.ToLocalTime();
            item.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<BasicEnumItem>().Add(item);
            await _dbContext.SaveChangesAsync(ct);
            return item;
        }

        public async Task<BasicEnumItem?> UpdateEnumItemAsync(BasicEnumItem item, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<BasicEnumItem>().FirstOrDefaultAsync(i => i.Id == item.Id, ct);
            if (existing == null) return null;

            existing.Name = item.Name;
            existing.DisplayName = item.DisplayName;
            existing.Code = item.Code;
            existing.Description = item.Description;
            existing.IsEnabled = item.IsEnabled;
            existing.SortOrder = item.SortOrder;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteEnumItemAsync(int id, CancellationToken ct = default)
        {
            var item = await _dbContext.Set<BasicEnumItem>().FirstOrDefaultAsync(i => i.Id == id, ct);
            if (item == null) return false;
            _dbContext.Set<BasicEnumItem>().Remove(item);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }
    }
}
