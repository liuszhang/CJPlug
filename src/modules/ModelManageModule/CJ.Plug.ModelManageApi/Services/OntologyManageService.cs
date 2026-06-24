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
                .OrderBy(o => o.SortOrder)
                .ToListAsync(ct);
        }

        public async Task<IEnumerable<Ontology>> GetOntologyTreeAsync(CancellationToken ct = default)
        {
            var all = await _dbContext.Set<Ontology>()
                .Include(o => o.Properties.OrderBy(p => p.SortOrder))
                .Include(o => o.Children.OrderBy(c => c.SortOrder))
                .OrderBy(o => o.SortOrder)
                .ToListAsync(ct);

            // 返回根节点（ParentId 为 null 的），Children 由 Include 自动填充
            return all.Where(o => o.ParentId == null).ToList();
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
            existing.Length = property.Length;
            existing.DictCode = property.DictCode;
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
            existing.ActionType = behavior.ActionType;
            existing.ApiUrl = behavior.ApiUrl;
            existing.ConfirmMessage = behavior.ConfirmMessage;
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
            existing.RuleExpression = rule.RuleExpression;
            existing.RuleCondition = rule.RuleCondition;
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

        // ========== 属性约束 CRUD ==========
        public async Task<IEnumerable<PropertyConstraint>> GetConstraintsByPropertyIdAsync(int propertyId, CancellationToken ct = default)
        {
            return await _dbContext.Set<PropertyConstraint>()
                .Where(c => c.PropertyId == propertyId)
                .OrderBy(c => c.SortOrder)
                .ToListAsync(ct);
        }

        public async Task<PropertyConstraint?> CreateConstraintAsync(PropertyConstraint constraint, CancellationToken ct = default)
        {
            _dbContext.Set<PropertyConstraint>().Add(constraint);
            await _dbContext.SaveChangesAsync(ct);
            return constraint;
        }

        public async Task<PropertyConstraint?> UpdateConstraintAsync(PropertyConstraint constraint, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<PropertyConstraint>().FirstOrDefaultAsync(c => c.Id == constraint.Id, ct);
            if (existing == null) return null;

            existing.ConstraintType = constraint.ConstraintType;
            existing.Value = constraint.Value;
            existing.Message = constraint.Message;
            existing.SortOrder = constraint.SortOrder;
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteConstraintAsync(int id, CancellationToken ct = default)
        {
            var constraint = await _dbContext.Set<PropertyConstraint>().FirstOrDefaultAsync(c => c.Id == id, ct);
            if (constraint == null) return false;
            _dbContext.Set<PropertyConstraint>().Remove(constraint);
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

        // ========== SecurityLevelAccess CRUD ==========
        public async Task<IEnumerable<SecurityLevelAccess>> GetSecurityLevelAccessByPersonnelAsync(int personnelLevelItemId, CancellationToken ct = default)
        {
            return await _dbContext.Set<SecurityLevelAccess>()
                .Where(a => a.PersonnelLevelItemId == personnelLevelItemId)
                .Include(a => a.DataLevelItem)
                .ToListAsync(ct);
        }

        public async Task<bool> UpdateSecurityLevelAccessAsync(int personnelLevelItemId, List<int> dataLevelItemIds, CancellationToken ct = default)
        {
            // 删除旧记录
            var oldItems = await _dbContext.Set<SecurityLevelAccess>()
                .Where(a => a.PersonnelLevelItemId == personnelLevelItemId)
                .ToListAsync(ct);
            _dbContext.Set<SecurityLevelAccess>().RemoveRange(oldItems);

            // 添加新记录
            if (dataLevelItemIds.Any())
            {
                var newItems = dataLevelItemIds.Distinct().Select(dId => new SecurityLevelAccess
                {
                    PersonnelLevelItemId = personnelLevelItemId,
                    DataLevelItemId = dId,
                });
                _dbContext.Set<SecurityLevelAccess>().AddRange(newItems);
            }

            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteSecurityLevelAccessAsync(int personnelLevelItemId, CancellationToken ct = default)
        {
            var items = await _dbContext.Set<SecurityLevelAccess>()
                .Where(a => a.PersonnelLevelItemId == personnelLevelItemId)
                .ToListAsync(ct);
            if (!items.Any()) return false;
            _dbContext.Set<SecurityLevelAccess>().RemoveRange(items);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== M4 场景 CRUD ==========
        public async Task<IEnumerable<Scenario>> GetAllScenariosAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<Scenario>()
                .OrderBy(s => s.Code)
                .ToListAsync(ct);
        }

        public async Task<Scenario?> GetScenarioByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<Scenario>().FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        public async Task<Scenario?> CreateScenarioAsync(Scenario scenario, CancellationToken ct = default)
        {
            scenario.CreatedAt = DateTime.UtcNow.ToLocalTime();
            scenario.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<Scenario>().Add(scenario);
            await _dbContext.SaveChangesAsync(ct);
            return scenario;
        }

        public async Task<Scenario?> UpdateScenarioAsync(Scenario scenario, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<Scenario>().FirstOrDefaultAsync(s => s.Id == scenario.Id, ct);
            if (existing == null) return null;

            existing.Name = scenario.Name;
            existing.Code = scenario.Code;
            existing.Description = scenario.Description;
            existing.OntologyId = scenario.OntologyId;
            existing.Steps = scenario.Steps;
            existing.IsActive = scenario.IsActive;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteScenarioAsync(int id, CancellationToken ct = default)
        {
            var entity = await _dbContext.Set<Scenario>().FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entity == null) return false;
            _dbContext.Set<Scenario>().Remove(entity);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== M5 主体 CRUD ==========
        public async Task<IEnumerable<Subject>> GetAllSubjectsAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<Subject>()
                .Include(s => s.Permissions)
                .OrderBy(s => s.Code)
                .ToListAsync(ct);
        }

        public async Task<Subject?> GetSubjectByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<Subject>()
                .Include(s => s.Children)
                .Include(s => s.Permissions)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        public async Task<IEnumerable<Subject>> GetChildrenByParentIdAsync(int parentId, CancellationToken ct = default)
        {
            return await _dbContext.Set<Subject>()
                .Where(s => s.ParentId == parentId)
                .OrderBy(s => s.Code)
                .ToListAsync(ct);
        }

        public async Task<Subject?> CreateSubjectAsync(Subject subject, CancellationToken ct = default)
        {
            subject.CreatedAt = DateTime.UtcNow.ToLocalTime();
            subject.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<Subject>().Add(subject);
            await _dbContext.SaveChangesAsync(ct);
            return subject;
        }

        public async Task<Subject?> UpdateSubjectAsync(Subject subject, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<Subject>().FirstOrDefaultAsync(s => s.Id == subject.Id, ct);
            if (existing == null) return null;

            existing.Name = subject.Name;
            existing.Code = subject.Code;
            existing.SubjectType = subject.SubjectType;
            existing.Description = subject.Description;
            existing.ParentId = subject.ParentId;
            existing.OntologyId = subject.OntologyId;
            existing.Properties = subject.Properties;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteSubjectAsync(int id, CancellationToken ct = default)
        {
            var entity = await _dbContext.Set<Subject>().FirstOrDefaultAsync(s => s.Id == id, ct);
            if (entity == null) return false;
            _dbContext.Set<Subject>().Remove(entity);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== M5 权限 CRUD ==========
        public async Task<IEnumerable<Permission>> GetAllPermissionsAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<Permission>()
                .OrderBy(p => p.Code)
                .ToListAsync(ct);
        }

        public async Task<Permission?> GetPermissionByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<Permission>().FirstOrDefaultAsync(p => p.Id == id, ct);
        }

        public async Task<IEnumerable<Permission>> GetPermissionsBySubjectIdAsync(int subjectId, CancellationToken ct = default)
        {
            return await _dbContext.Set<Permission>()
                .Where(p => p.SubjectId == subjectId)
                .OrderBy(p => p.Code)
                .ToListAsync(ct);
        }

        public async Task<Permission?> CreatePermissionAsync(Permission permission, CancellationToken ct = default)
        {
            permission.CreatedAt = DateTime.UtcNow.ToLocalTime();
            permission.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<Permission>().Add(permission);
            await _dbContext.SaveChangesAsync(ct);
            return permission;
        }

        public async Task<Permission?> UpdatePermissionAsync(Permission permission, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<Permission>().FirstOrDefaultAsync(p => p.Id == permission.Id, ct);
            if (existing == null) return null;

            existing.Name = permission.Name;
            existing.Code = permission.Code;
            existing.ResourceType = permission.ResourceType;
            existing.ResourceId = permission.ResourceId;
            existing.Action = permission.Action;
            existing.SubjectId = permission.SubjectId;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeletePermissionAsync(int id, CancellationToken ct = default)
        {
            var entity = await _dbContext.Set<Permission>().FirstOrDefaultAsync(p => p.Id == id, ct);
            if (entity == null) return false;
            _dbContext.Set<Permission>().Remove(entity);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== M5.5 外部系统 CRUD ==========
        public async Task<IEnumerable<ExternalSystem>> GetAllExternalSystemsAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<ExternalSystem>()
                .Include(es => es.InterfaceContracts)
                .OrderBy(es => es.Code)
                .ToListAsync(ct);
        }

        public async Task<ExternalSystem?> GetExternalSystemByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<ExternalSystem>()
                .Include(es => es.InterfaceContracts)
                .FirstOrDefaultAsync(es => es.Id == id, ct);
        }

        public async Task<ExternalSystem?> CreateExternalSystemAsync(ExternalSystem externalSystem, CancellationToken ct = default)
        {
            externalSystem.CreatedAt = DateTime.UtcNow.ToLocalTime();
            externalSystem.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<ExternalSystem>().Add(externalSystem);
            await _dbContext.SaveChangesAsync(ct);
            return externalSystem;
        }

        public async Task<ExternalSystem?> UpdateExternalSystemAsync(ExternalSystem externalSystem, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<ExternalSystem>().FirstOrDefaultAsync(es => es.Id == externalSystem.Id, ct);
            if (existing == null) return null;

            existing.Name = externalSystem.Name;
            existing.Code = externalSystem.Code;
            existing.Description = externalSystem.Description;
            existing.BaseUrl = externalSystem.BaseUrl;
            existing.AuthType = externalSystem.AuthType;
            existing.AuthConfig = externalSystem.AuthConfig;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteExternalSystemAsync(int id, CancellationToken ct = default)
        {
            var entity = await _dbContext.Set<ExternalSystem>().FirstOrDefaultAsync(es => es.Id == id, ct);
            if (entity == null) return false;
            _dbContext.Set<ExternalSystem>().Remove(entity);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== M5.5 接口契约 CRUD ==========
        public async Task<IEnumerable<InterfaceContract>> GetAllInterfaceContractsAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<InterfaceContract>()
                .OrderBy(ic => ic.Code)
                .ToListAsync(ct);
        }

        public async Task<InterfaceContract?> GetInterfaceContractByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<InterfaceContract>().FirstOrDefaultAsync(ic => ic.Id == id, ct);
        }

        public async Task<IEnumerable<InterfaceContract>> GetInterfaceContractsByExternalSystemIdAsync(int externalSystemId, CancellationToken ct = default)
        {
            return await _dbContext.Set<InterfaceContract>()
                .Where(ic => ic.ExternalSystemId == externalSystemId)
                .OrderBy(ic => ic.Code)
                .ToListAsync(ct);
        }

        public async Task<InterfaceContract?> CreateInterfaceContractAsync(InterfaceContract interfaceContract, CancellationToken ct = default)
        {
            interfaceContract.CreatedAt = DateTime.UtcNow.ToLocalTime();
            interfaceContract.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<InterfaceContract>().Add(interfaceContract);
            await _dbContext.SaveChangesAsync(ct);
            return interfaceContract;
        }

        public async Task<InterfaceContract?> UpdateInterfaceContractAsync(InterfaceContract interfaceContract, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<InterfaceContract>().FirstOrDefaultAsync(ic => ic.Id == interfaceContract.Id, ct);
            if (existing == null) return null;

            existing.Name = interfaceContract.Name;
            existing.Code = interfaceContract.Code;
            existing.Description = interfaceContract.Description;
            existing.ExternalSystemId = interfaceContract.ExternalSystemId;
            existing.Method = interfaceContract.Method;
            existing.Endpoint = interfaceContract.Endpoint;
            existing.RequestSchema = interfaceContract.RequestSchema;
            existing.ResponseSchema = interfaceContract.ResponseSchema;
            existing.Status = interfaceContract.Status;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteInterfaceContractAsync(int id, CancellationToken ct = default)
        {
            var entity = await _dbContext.Set<InterfaceContract>().FirstOrDefaultAsync(ic => ic.Id == id, ct);
            if (entity == null) return false;
            _dbContext.Set<InterfaceContract>().Remove(entity);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== M6 异常类型 CRUD ==========
        public async Task<IEnumerable<ExceptionType>> GetAllExceptionTypesAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<ExceptionType>()
                .Include(et => et.CompensationActions)
                .OrderBy(et => et.Code)
                .ToListAsync(ct);
        }

        public async Task<ExceptionType?> GetExceptionTypeByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<ExceptionType>()
                .Include(et => et.CompensationActions)
                .FirstOrDefaultAsync(et => et.Id == id, ct);
        }

        public async Task<ExceptionType?> CreateExceptionTypeAsync(ExceptionType exceptionType, CancellationToken ct = default)
        {
            exceptionType.CreatedAt = DateTime.UtcNow.ToLocalTime();
            exceptionType.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<ExceptionType>().Add(exceptionType);
            await _dbContext.SaveChangesAsync(ct);
            return exceptionType;
        }

        public async Task<ExceptionType?> UpdateExceptionTypeAsync(ExceptionType exceptionType, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<ExceptionType>().FirstOrDefaultAsync(et => et.Id == exceptionType.Id, ct);
            if (existing == null) return null;

            existing.Name = exceptionType.Name;
            existing.Code = exceptionType.Code;
            existing.Description = exceptionType.Description;
            existing.Severity = exceptionType.Severity;
            existing.Category = exceptionType.Category;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteExceptionTypeAsync(int id, CancellationToken ct = default)
        {
            var entity = await _dbContext.Set<ExceptionType>().FirstOrDefaultAsync(et => et.Id == id, ct);
            if (entity == null) return false;
            _dbContext.Set<ExceptionType>().Remove(entity);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== M6 补偿动作 CRUD ==========
        public async Task<IEnumerable<CompensationAction>> GetAllCompensationActionsAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<CompensationAction>()
                .OrderBy(ca => ca.Code)
                .ToListAsync(ct);
        }

        public async Task<CompensationAction?> GetCompensationActionByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<CompensationAction>().FirstOrDefaultAsync(ca => ca.Id == id, ct);
        }

        public async Task<IEnumerable<CompensationAction>> GetCompensationActionsByExceptionTypeIdAsync(int exceptionTypeId, CancellationToken ct = default)
        {
            return await _dbContext.Set<CompensationAction>()
                .Where(ca => ca.ExceptionTypeId == exceptionTypeId)
                .OrderBy(ca => ca.Code)
                .ToListAsync(ct);
        }

        public async Task<CompensationAction?> CreateCompensationActionAsync(CompensationAction compensationAction, CancellationToken ct = default)
        {
            compensationAction.CreatedAt = DateTime.UtcNow.ToLocalTime();
            compensationAction.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<CompensationAction>().Add(compensationAction);
            await _dbContext.SaveChangesAsync(ct);
            return compensationAction;
        }

        public async Task<CompensationAction?> UpdateCompensationActionAsync(CompensationAction compensationAction, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<CompensationAction>().FirstOrDefaultAsync(ca => ca.Id == compensationAction.Id, ct);
            if (existing == null) return null;

            existing.Name = compensationAction.Name;
            existing.Code = compensationAction.Code;
            existing.Description = compensationAction.Description;
            existing.ExceptionTypeId = compensationAction.ExceptionTypeId;
            existing.ActionType = compensationAction.ActionType;
            existing.ActionConfig = compensationAction.ActionConfig;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteCompensationActionAsync(int id, CancellationToken ct = default)
        {
            var entity = await _dbContext.Set<CompensationAction>().FirstOrDefaultAsync(ca => ca.Id == id, ct);
            if (entity == null) return false;
            _dbContext.Set<CompensationAction>().Remove(entity);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== M7 质量指标 CRUD ==========
        public async Task<IEnumerable<QualityMetric>> GetAllQualityMetricsAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<QualityMetric>()
                .Include(qm => qm.AlertRules)
                .Include(qm => qm.ImprovementActions)
                .OrderBy(qm => qm.Code)
                .ToListAsync(ct);
        }

        public async Task<QualityMetric?> GetQualityMetricByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<QualityMetric>()
                .Include(qm => qm.AlertRules)
                .Include(qm => qm.ImprovementActions)
                .FirstOrDefaultAsync(qm => qm.Id == id, ct);
        }

        public async Task<QualityMetric?> CreateQualityMetricAsync(QualityMetric qualityMetric, CancellationToken ct = default)
        {
            qualityMetric.CreatedAt = DateTime.UtcNow.ToLocalTime();
            qualityMetric.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<QualityMetric>().Add(qualityMetric);
            await _dbContext.SaveChangesAsync(ct);
            return qualityMetric;
        }

        public async Task<QualityMetric?> UpdateQualityMetricAsync(QualityMetric qualityMetric, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<QualityMetric>().FirstOrDefaultAsync(qm => qm.Id == qualityMetric.Id, ct);
            if (existing == null) return null;

            existing.Name = qualityMetric.Name;
            existing.Code = qualityMetric.Code;
            existing.Description = qualityMetric.Description;
            existing.Unit = qualityMetric.Unit;
            existing.TargetValue = qualityMetric.TargetValue;
            existing.CurrentValue = qualityMetric.CurrentValue;
            existing.MeasureType = qualityMetric.MeasureType;
            existing.OntologyId = qualityMetric.OntologyId;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteQualityMetricAsync(int id, CancellationToken ct = default)
        {
            var entity = await _dbContext.Set<QualityMetric>().FirstOrDefaultAsync(qm => qm.Id == id, ct);
            if (entity == null) return false;
            _dbContext.Set<QualityMetric>().Remove(entity);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== M7 告警规则 CRUD ==========
        public async Task<IEnumerable<AlertRule>> GetAllAlertRulesAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<AlertRule>()
                .OrderBy(ar => ar.Code)
                .ToListAsync(ct);
        }

        public async Task<AlertRule?> GetAlertRuleByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<AlertRule>().FirstOrDefaultAsync(ar => ar.Id == id, ct);
        }

        public async Task<IEnumerable<AlertRule>> GetAlertRulesByQualityMetricIdAsync(int qualityMetricId, CancellationToken ct = default)
        {
            return await _dbContext.Set<AlertRule>()
                .Where(ar => ar.QualityMetricId == qualityMetricId)
                .OrderBy(ar => ar.Code)
                .ToListAsync(ct);
        }

        public async Task<AlertRule?> CreateAlertRuleAsync(AlertRule alertRule, CancellationToken ct = default)
        {
            alertRule.CreatedAt = DateTime.UtcNow.ToLocalTime();
            alertRule.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<AlertRule>().Add(alertRule);
            await _dbContext.SaveChangesAsync(ct);
            return alertRule;
        }

        public async Task<AlertRule?> UpdateAlertRuleAsync(AlertRule alertRule, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<AlertRule>().FirstOrDefaultAsync(ar => ar.Id == alertRule.Id, ct);
            if (existing == null) return null;

            existing.Name = alertRule.Name;
            existing.Code = alertRule.Code;
            existing.Description = alertRule.Description;
            existing.QualityMetricId = alertRule.QualityMetricId;
            existing.Condition = alertRule.Condition;
            existing.Threshold = alertRule.Threshold;
            existing.Severity = alertRule.Severity;
            existing.IsEnabled = alertRule.IsEnabled;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteAlertRuleAsync(int id, CancellationToken ct = default)
        {
            var entity = await _dbContext.Set<AlertRule>().FirstOrDefaultAsync(ar => ar.Id == id, ct);
            if (entity == null) return false;
            _dbContext.Set<AlertRule>().Remove(entity);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

        // ========== M7 改进措施 CRUD ==========
        public async Task<IEnumerable<ImprovementAction>> GetAllImprovementActionsAsync(CancellationToken ct = default)
        {
            return await _dbContext.Set<ImprovementAction>()
                .OrderBy(ia => ia.Code)
                .ToListAsync(ct);
        }

        public async Task<ImprovementAction?> GetImprovementActionByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Set<ImprovementAction>().FirstOrDefaultAsync(ia => ia.Id == id, ct);
        }

        public async Task<IEnumerable<ImprovementAction>> GetImprovementActionsByQualityMetricIdAsync(int qualityMetricId, CancellationToken ct = default)
        {
            return await _dbContext.Set<ImprovementAction>()
                .Where(ia => ia.QualityMetricId == qualityMetricId)
                .OrderBy(ia => ia.Code)
                .ToListAsync(ct);
        }

        public async Task<ImprovementAction?> CreateImprovementActionAsync(ImprovementAction improvementAction, CancellationToken ct = default)
        {
            improvementAction.CreatedAt = DateTime.UtcNow.ToLocalTime();
            improvementAction.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            _dbContext.Set<ImprovementAction>().Add(improvementAction);
            await _dbContext.SaveChangesAsync(ct);
            return improvementAction;
        }

        public async Task<ImprovementAction?> UpdateImprovementActionAsync(ImprovementAction improvementAction, CancellationToken ct = default)
        {
            var existing = await _dbContext.Set<ImprovementAction>().FirstOrDefaultAsync(ia => ia.Id == improvementAction.Id, ct);
            if (existing == null) return null;

            existing.Name = improvementAction.Name;
            existing.Code = improvementAction.Code;
            existing.Description = improvementAction.Description;
            existing.QualityMetricId = improvementAction.QualityMetricId;
            existing.ProposedBy = improvementAction.ProposedBy;
            existing.Status = improvementAction.Status;
            existing.Result = improvementAction.Result;
            existing.UpdatedAt = DateTime.UtcNow.ToLocalTime();
            await _dbContext.SaveChangesAsync(ct);
            return existing;
        }

        public async Task<bool> DeleteImprovementActionAsync(int id, CancellationToken ct = default)
        {
            var entity = await _dbContext.Set<ImprovementAction>().FirstOrDefaultAsync(ia => ia.Id == id, ct);
            if (entity == null) return false;
            _dbContext.Set<ImprovementAction>().Remove(entity);
            await _dbContext.SaveChangesAsync(ct);
            return true;
        }

    }
}