using CJ.Plug.ModelManageApiClient;
using CJ.Plug.ModelManageModel.Models;
using System.Net.Http.Json;

namespace CJ.Plug.ModelManageApiClient
{
    public partial class OntologyManageApiClient : BaseApiClient, IOntologyManageApiClient
    {
        public OntologyManageApiClient(HttpClient dispatcherClient) : base(dispatcherClient) { }

        // ========== 本体 CRUD ==========
        public async Task<IEnumerable<Ontology?>> GetAllOntologiesAsync(CancellationToken ct = default)
        {
            return await httpClient.GetFromJsonAsync<List<Ontology>>("/api/ontology/getAll", ct) ?? new List<Ontology>();
        }

        public async Task<Ontology?> GetOntologyByIdAsync(int id, CancellationToken ct = default)
        {
            return await httpClient.GetFromJsonAsync<Ontology>($"/api/ontology/getById/{id}", ct);
        }

        public async Task<Ontology?> CreateOntologyAsync(Ontology ontology, CancellationToken ct = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/ontology/create", ontology, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Ontology>(cancellationToken: ct);
        }

        public async Task<Ontology?> UpdateOntologyAsync(Ontology ontology, CancellationToken ct = default)
        {
            var response = await httpClient.PutAsJsonAsync("/api/ontology/update", ontology, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Ontology>(cancellationToken: ct);
        }

        public async Task<bool> DeleteOntologyAsync(int id, CancellationToken ct = default)
        {
            var response = await httpClient.DeleteAsync($"/api/ontology/delete/{id}", ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: ct);
        }

        // ========== 属性 CRUD ==========
        public async Task<IEnumerable<Property?>> GetPropertiesByOntologyIdAsync(int ontologyId, CancellationToken ct = default)
        {
            return await httpClient.GetFromJsonAsync<List<Property>>($"/api/ontology/properties/{ontologyId}", ct) ?? new List<Property>();
        }

        public async Task<Property?> CreatePropertyAsync(Property property, CancellationToken ct = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/ontology/properties/create", property, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Property>(cancellationToken: ct);
        }

        public async Task<Property?> UpdatePropertyAsync(Property property, CancellationToken ct = default)
        {
            var response = await httpClient.PutAsJsonAsync("/api/ontology/properties/update", property, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Property>(cancellationToken: ct);
        }

        public async Task<bool> DeletePropertyAsync(int id, CancellationToken ct = default)
        {
            var response = await httpClient.DeleteAsync($"/api/ontology/properties/delete/{id}", ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: ct);
        }

        public async Task<bool> ReorderPropertiesAsync(int ontologyId, List<int> propertyIds, CancellationToken ct = default)
        {
            var response = await httpClient.PutAsJsonAsync($"/api/ontology/properties/reorder/{ontologyId}", propertyIds, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: ct);
        }

        // ========== 属性约束 CRUD ==========
        public async Task<IEnumerable<PropertyConstraint?>> GetConstraintsByPropertyIdAsync(int propertyId, CancellationToken ct = default)
        {
            return await httpClient.GetFromJsonAsync<List<PropertyConstraint>>($"/api/ontology/properties/{propertyId}/constraints", ct) ?? new List<PropertyConstraint>();
        }

        public async Task<PropertyConstraint?> CreateConstraintAsync(int propertyId, PropertyConstraint constraint, CancellationToken ct = default)
        {
            var response = await httpClient.PostAsJsonAsync($"/api/ontology/properties/{propertyId}/constraints", constraint, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PropertyConstraint>(cancellationToken: ct);
        }

        public async Task<PropertyConstraint?> UpdateConstraintAsync(int constraintId, PropertyConstraint constraint, CancellationToken ct = default)
        {
            var response = await httpClient.PutAsJsonAsync($"/api/ontology/properties/constraints/{constraintId}", constraint, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PropertyConstraint>(cancellationToken: ct);
        }

        public async Task<bool> DeleteConstraintAsync(int constraintId, CancellationToken ct = default)
        {
            var response = await httpClient.DeleteAsync($"/api/ontology/properties/constraints/{constraintId}", ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: ct);
        }

        // ========== 关系 CRUD ==========
        public async Task<IEnumerable<OntologyRelationship?>> GetRelationshipsByOntologyIdAsync(int ontologyId, CancellationToken ct = default)
        {
            return await httpClient.GetFromJsonAsync<List<OntologyRelationship>>($"/api/ontology/relationships/{ontologyId}", ct) ?? new List<OntologyRelationship>();
        }

        public async Task<IEnumerable<OntologyRelationship?>> GetAllRelationshipsAsync(CancellationToken ct = default)
        {
            return await httpClient.GetFromJsonAsync<List<OntologyRelationship>>("/api/ontology/relationships/all", ct) ?? new List<OntologyRelationship>();
        }

        public async Task<OntologyRelationship?> CreateRelationshipAsync(OntologyRelationship relationship, CancellationToken ct = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/ontology/relationships/create", relationship, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OntologyRelationship>(cancellationToken: ct);
        }

        public async Task<OntologyRelationship?> UpdateRelationshipAsync(OntologyRelationship relationship, CancellationToken ct = default)
        {
            var response = await httpClient.PutAsJsonAsync("/api/ontology/relationships/update", relationship, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OntologyRelationship>(cancellationToken: ct);
        }

        public async Task<bool> DeleteRelationshipAsync(int id, CancellationToken ct = default)
        {
            var response = await httpClient.DeleteAsync($"/api/ontology/relationships/delete/{id}", ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: ct);
        }

        // ========== 行为 CRUD ==========
        public async Task<IEnumerable<ObjectBehavior?>> GetBehaviorsByOntologyIdAsync(int ontologyId, CancellationToken ct = default)
        {
            return await httpClient.GetFromJsonAsync<List<ObjectBehavior>>($"/api/ontology/behaviors/{ontologyId}", ct) ?? new List<ObjectBehavior>();
        }

        public async Task<IEnumerable<ObjectBehavior?>> GetAllBehaviorsAsync(CancellationToken ct = default)
        {
            return await httpClient.GetFromJsonAsync<List<ObjectBehavior>>("/api/ontology/behaviors/all", ct) ?? new List<ObjectBehavior>();
        }

        public async Task<ObjectBehavior?> CreateBehaviorAsync(ObjectBehavior behavior, List<int>? ruleIds = null, CancellationToken ct = default)
        {
            var request = new { Behavior = behavior, RuleIds = ruleIds };
            var response = await httpClient.PostAsJsonAsync("/api/ontology/behaviors/create", request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ObjectBehavior>(cancellationToken: ct);
        }

        public async Task<ObjectBehavior?> UpdateBehaviorAsync(ObjectBehavior behavior, List<int>? ruleIds = null, CancellationToken ct = default)
        {
            var request = new { Behavior = behavior, RuleIds = ruleIds };
            var response = await httpClient.PutAsJsonAsync("/api/ontology/behaviors/update", request, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ObjectBehavior>(cancellationToken: ct);
        }

        public async Task<bool> DeleteBehaviorAsync(int id, CancellationToken ct = default)
        {
            var response = await httpClient.DeleteAsync($"/api/ontology/behaviors/delete/{id}", ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: ct);
        }

        public async Task<IEnumerable<OntologyRule?>> GetRulesByBehaviorIdAsync(int behaviorId, CancellationToken ct = default)
        {
            return await httpClient.GetFromJsonAsync<List<OntologyRule>>($"/api/ontology/behaviors/{behaviorId}/rules", ct) ?? new List<OntologyRule>();
        }

        // ========== 规则 CRUD ==========
        public async Task<IEnumerable<OntologyRule?>> GetRulesByOntologyIdAsync(int ontologyId, CancellationToken ct = default)
        {
            return await httpClient.GetFromJsonAsync<List<OntologyRule>>($"/api/ontology/rules/{ontologyId}", ct) ?? new List<OntologyRule>();
        }

        public async Task<IEnumerable<OntologyRule?>> GetAllRulesAsync(CancellationToken ct = default)
        {
            return await httpClient.GetFromJsonAsync<List<OntologyRule>>("/api/ontology/rules/all", ct) ?? new List<OntologyRule>();
        }

        public async Task<OntologyRule?> CreateRuleAsync(OntologyRule rule, int? associatedOntologyId = null, int? associatedBehaviorId = null, CancellationToken ct = default)
        {
            var queryParams = new List<string>();
            if (associatedOntologyId.HasValue) queryParams.Add($"associatedOntologyId={associatedOntologyId.Value}");
            if (associatedBehaviorId.HasValue) queryParams.Add($"associatedBehaviorId={associatedBehaviorId.Value}");
            var url = "/api/ontology/rules/create";
            if (queryParams.Any()) url += "?" + string.Join("&", queryParams);
            var response = await httpClient.PostAsJsonAsync(url, rule, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OntologyRule>(cancellationToken: ct);
        }

        public async Task<OntologyRule?> UpdateRuleAsync(OntologyRule rule, CancellationToken ct = default)
        {
            var response = await httpClient.PutAsJsonAsync("/api/ontology/rules/update", rule, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OntologyRule>(cancellationToken: ct);
        }

        public async Task<bool> DeleteRuleAsync(int id, CancellationToken ct = default)
        {
            var response = await httpClient.DeleteAsync($"/api/ontology/rules/delete/{id}", ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: ct);
        }

        // ========== 基础枚举 CRUD ==========
        public async Task<IEnumerable<BasicEnum?>> GetAllBasicEnumsAsync(CancellationToken ct = default)
        {
            return await httpClient.GetFromJsonAsync<List<BasicEnum>>("/api/ontology/basic-enums/all", ct) ?? new List<BasicEnum>();
        }

        public async Task<BasicEnum?> GetBasicEnumByIdAsync(int id, CancellationToken ct = default)
        {
            return await httpClient.GetFromJsonAsync<BasicEnum>($"/api/ontology/basic-enums/{id}", ct);
        }

        public async Task<BasicEnum?> CreateBasicEnumAsync(BasicEnum basicEnum, CancellationToken ct = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/ontology/basic-enums/create", basicEnum, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BasicEnum>(cancellationToken: ct);
        }

        public async Task<BasicEnum?> UpdateBasicEnumAsync(BasicEnum basicEnum, CancellationToken ct = default)
        {
            var response = await httpClient.PutAsJsonAsync("/api/ontology/basic-enums/update", basicEnum, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BasicEnum>(cancellationToken: ct);
        }

        public async Task<bool> DeleteBasicEnumAsync(int id, CancellationToken ct = default)
        {
            var response = await httpClient.DeleteAsync($"/api/ontology/basic-enums/delete/{id}", ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: ct);
        }

        // ========== 基础枚举项 CRUD ==========
        public async Task<IEnumerable<BasicEnumItem?>> GetEnumItemsAsync(int enumId, CancellationToken ct = default)
        {
            return await httpClient.GetFromJsonAsync<List<BasicEnumItem>>($"/api/ontology/basic-enums/{enumId}/items", ct) ?? new List<BasicEnumItem>();
        }

        public async Task<BasicEnumItem?> CreateEnumItemAsync(BasicEnumItem item, CancellationToken ct = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/ontology/basic-enums/items/create", item, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BasicEnumItem>(cancellationToken: ct);
        }

        public async Task<BasicEnumItem?> UpdateEnumItemAsync(BasicEnumItem item, CancellationToken ct = default)
        {
            var response = await httpClient.PutAsJsonAsync("/api/ontology/basic-enums/items/update", item, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<BasicEnumItem>(cancellationToken: ct);
        }

        public async Task<bool> DeleteEnumItemAsync(int id, CancellationToken ct = default)
        {
            var response = await httpClient.DeleteAsync($"/api/ontology/basic-enums/items/delete/{id}", ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: ct);
        }
    }
}
