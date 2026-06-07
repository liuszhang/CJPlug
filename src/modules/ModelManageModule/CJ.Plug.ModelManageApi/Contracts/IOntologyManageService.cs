using CJ.Plug.ModelManageModel.Models;

namespace CJ.Plug.ModelManageApi.Contracts
{
    public interface IOntologyManageService
    {
        // 本体 CRUD
        Task<IEnumerable<Ontology>> GetAllOntologiesAsync(CancellationToken ct = default);
        Task<Ontology?> GetOntologyByIdAsync(int id, CancellationToken ct = default);
        Task<Ontology?> CreateOntologyAsync(Ontology ontology, CancellationToken ct = default);
        Task<Ontology?> UpdateOntologyAsync(Ontology ontology, CancellationToken ct = default);
        Task<bool> DeleteOntologyAsync(int id, CancellationToken ct = default);

        // 属性 CRUD
        Task<IEnumerable<Property>> GetPropertiesByOntologyIdAsync(int ontologyId, CancellationToken ct = default);
        Task<Property?> GetPropertyByIdAsync(int id, CancellationToken ct = default);
        Task<Property?> CreatePropertyAsync(Property property, CancellationToken ct = default);
        Task<Property?> UpdatePropertyAsync(Property property, CancellationToken ct = default);
        Task<bool> DeletePropertyAsync(int id, CancellationToken ct = default);
        Task<bool> ReorderPropertiesAsync(int ontologyId, List<int> propertyIds, CancellationToken ct = default);

        // 关系 CRUD
        Task<IEnumerable<OntologyRelationship>> GetRelationshipsByOntologyIdAsync(int ontologyId, CancellationToken ct = default);
        Task<IEnumerable<OntologyRelationship>> GetAllRelationshipsAsync(CancellationToken ct = default);
        Task<OntologyRelationship?> GetRelationshipByIdAsync(int id, CancellationToken ct = default);
        Task<OntologyRelationship?> CreateRelationshipAsync(OntologyRelationship relationship, CancellationToken ct = default);
        Task<OntologyRelationship?> UpdateRelationshipAsync(OntologyRelationship relationship, CancellationToken ct = default);
        Task<bool> DeleteRelationshipAsync(int id, CancellationToken ct = default);

        // 行为 CRUD
        Task<IEnumerable<ObjectBehavior>> GetBehaviorsByOntologyIdAsync(int ontologyId, CancellationToken ct = default);
        Task<IEnumerable<ObjectBehavior>> GetAllBehaviorsAsync(CancellationToken ct = default);
        Task<ObjectBehavior?> GetBehaviorByIdAsync(int id, CancellationToken ct = default);
        Task<ObjectBehavior?> CreateBehaviorAsync(ObjectBehavior behavior, List<int>? ruleIds = null, CancellationToken ct = default);
        Task<ObjectBehavior?> UpdateBehaviorAsync(ObjectBehavior behavior, List<int>? ruleIds = null, CancellationToken ct = default);
        Task<bool> DeleteBehaviorAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<OntologyRule>> GetRulesByBehaviorIdAsync(int behaviorId, CancellationToken ct = default);

        // 规则 CRUD
        Task<IEnumerable<OntologyRule>> GetRulesByOntologyIdAsync(int ontologyId, CancellationToken ct = default);
        Task<IEnumerable<OntologyRule>> GetAllRulesAsync(CancellationToken ct = default);
        Task<OntologyRule?> GetRuleByIdAsync(int id, CancellationToken ct = default);
        Task<OntologyRule?> CreateRuleAsync(OntologyRule rule, int? associatedOntologyId = null, int? associatedBehaviorId = null, CancellationToken ct = default);
        Task<OntologyRule?> UpdateRuleAsync(OntologyRule rule, CancellationToken ct = default);
        Task<bool> DeleteRuleAsync(int id, CancellationToken ct = default);

        // 基础枚举 CRUD
        Task<IEnumerable<BasicEnum>> GetAllBasicEnumsAsync(CancellationToken ct = default);
        Task<BasicEnum?> GetBasicEnumByIdAsync(int id, CancellationToken ct = default);
        Task<BasicEnum?> CreateBasicEnumAsync(BasicEnum basicEnum, CancellationToken ct = default);
        Task<BasicEnum?> UpdateBasicEnumAsync(BasicEnum basicEnum, CancellationToken ct = default);
        Task<bool> DeleteBasicEnumAsync(int id, CancellationToken ct = default);

        // 基础枚举项 CRUD
        Task<IEnumerable<BasicEnumItem>> GetEnumItemsAsync(int enumId, CancellationToken ct = default);
        Task<BasicEnumItem?> CreateEnumItemAsync(BasicEnumItem item, CancellationToken ct = default);
        Task<BasicEnumItem?> UpdateEnumItemAsync(BasicEnumItem item, CancellationToken ct = default);
        Task<bool> DeleteEnumItemAsync(int id, CancellationToken ct = default);
    }
}
