using CJ.Plug.ModelManageApi.Contracts;
using CJ.Plug.ModelManageApi.Models;
using CJ.Plug.ModelManageModel.Models;
using Microsoft.AspNetCore.Mvc;

namespace CJ.Plug.ModelManageApi.Apis
{
    public static class OntologyManageApi
    {
        public static IEndpointRouteBuilder MapOntologyManageApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/ontology").WithTags("本体管理");

            // 本体 CRUD
            api.MapGet("/getAll", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllOntologiesAsync(ct));

            api.MapGet("/getById/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.GetOntologyByIdAsync(id, ct));

            api.MapPost("/create", async ([FromBody] Ontology ontology, IOntologyManageService service, CancellationToken ct) =>
                await service.CreateOntologyAsync(ontology, ct));

            api.MapPut("/update", async ([FromBody] Ontology ontology, IOntologyManageService service, CancellationToken ct) =>
                await service.UpdateOntologyAsync(ontology, ct));

            api.MapDelete("/delete/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteOntologyAsync(id, ct));

            // 属性 CRUD
            api.MapGet("/properties/{ontologyId}", async (int ontologyId, IOntologyManageService service, CancellationToken ct) =>
                await service.GetPropertiesByOntologyIdAsync(ontologyId, ct));

            api.MapPost("/properties/create", async ([FromBody] Property property, IOntologyManageService service, CancellationToken ct) =>
                await service.CreatePropertyAsync(property, ct));

            api.MapPut("/properties/update", async ([FromBody] Property property, IOntologyManageService service, CancellationToken ct) =>
                await service.UpdatePropertyAsync(property, ct));

            api.MapDelete("/properties/delete/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeletePropertyAsync(id, ct));

            api.MapPut("/properties/reorder/{ontologyId}", async (int ontologyId, [FromBody] List<int> propertyIds, IOntologyManageService service, CancellationToken ct) =>
                await service.ReorderPropertiesAsync(ontologyId, propertyIds, ct));

            // 属性约束 CRUD
            api.MapGet("/properties/{propertyId}/constraints", async (int propertyId, IOntologyManageService service, CancellationToken ct) =>
                await service.GetConstraintsByPropertyIdAsync(propertyId, ct));

            api.MapPost("/properties/{propertyId}/constraints", async (int propertyId, [FromBody] PropertyConstraint constraint, IOntologyManageService service, CancellationToken ct) =>
            {
                constraint.PropertyId = propertyId;
                return await service.CreateConstraintAsync(constraint, ct);
            });

            api.MapPut("/properties/constraints/{constraintId}", async (int constraintId, [FromBody] PropertyConstraint constraint, IOntologyManageService service, CancellationToken ct) =>
            {
                constraint.Id = constraintId;
                return await service.UpdateConstraintAsync(constraint, ct);
            });

            api.MapDelete("/properties/constraints/{constraintId}", async (int constraintId, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteConstraintAsync(constraintId, ct));

            // 关系 CRUD
            api.MapGet("/relationships/{ontologyId}", async (int ontologyId, IOntologyManageService service, CancellationToken ct) =>
                await service.GetRelationshipsByOntologyIdAsync(ontologyId, ct));

            api.MapGet("/relationships/all", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllRelationshipsAsync(ct));

            api.MapPost("/relationships/create", async ([FromBody] OntologyRelationship relationship, IOntologyManageService service, CancellationToken ct) =>
                await service.CreateRelationshipAsync(relationship, ct));

            api.MapPut("/relationships/update", async ([FromBody] OntologyRelationship relationship, IOntologyManageService service, CancellationToken ct) =>
                await service.UpdateRelationshipAsync(relationship, ct));

            api.MapDelete("/relationships/delete/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteRelationshipAsync(id, ct));

            // 行为 CRUD
            api.MapGet("/behaviors/{ontologyId}", async (int ontologyId, IOntologyManageService service, CancellationToken ct) =>
                await service.GetBehaviorsByOntologyIdAsync(ontologyId, ct));

            api.MapGet("/behaviors/all", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllBehaviorsAsync(ct));

            api.MapPost("/behaviors/create", async ([FromBody] CreateBehaviorRequest request, IOntologyManageService service, CancellationToken ct) =>
                await service.CreateBehaviorAsync(request.Behavior, request.RuleIds, ct));

            api.MapPut("/behaviors/update", async ([FromBody] CreateBehaviorRequest request, IOntologyManageService service, CancellationToken ct) =>
                await service.UpdateBehaviorAsync(request.Behavior, request.RuleIds, ct));

            api.MapDelete("/behaviors/delete/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteBehaviorAsync(id, ct));

            api.MapGet("/behaviors/{behaviorId}/rules", async (int behaviorId, IOntologyManageService service, CancellationToken ct) =>
                await service.GetRulesByBehaviorIdAsync(behaviorId, ct));

            // 规则 CRUD
            api.MapGet("/rules/{ontologyId}", async (int ontologyId, IOntologyManageService service, CancellationToken ct) =>
                await service.GetRulesByOntologyIdAsync(ontologyId, ct));

            api.MapGet("/rules/all", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllRulesAsync(ct));

            api.MapPost("/rules/create", async (
                [FromBody] OntologyRule rule,
                [FromQuery] int? associatedOntologyId,
                [FromQuery] int? associatedBehaviorId,
                IOntologyManageService service,
                CancellationToken ct) =>
                await service.CreateRuleAsync(rule, associatedOntologyId, associatedBehaviorId, ct));

            api.MapPut("/rules/update", async ([FromBody] OntologyRule rule, IOntologyManageService service, CancellationToken ct) =>
                await service.UpdateRuleAsync(rule, ct));

            api.MapDelete("/rules/delete/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteRuleAsync(id, ct));

            // 基础枚举 CRUD
            api.MapGet("/basic-enums/all", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllBasicEnumsAsync(ct));

            api.MapGet("/basic-enums/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.GetBasicEnumByIdAsync(id, ct));

            api.MapPost("/basic-enums/create", async ([FromBody] BasicEnum basicEnum, IOntologyManageService service, CancellationToken ct) =>
                await service.CreateBasicEnumAsync(basicEnum, ct));

            api.MapPut("/basic-enums/update", async ([FromBody] BasicEnum basicEnum, IOntologyManageService service, CancellationToken ct) =>
                await service.UpdateBasicEnumAsync(basicEnum, ct));

            api.MapDelete("/basic-enums/delete/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteBasicEnumAsync(id, ct));

            // 基础枚举项 CRUD
            api.MapGet("/basic-enums/{enumId}/items", async (int enumId, IOntologyManageService service, CancellationToken ct) =>
                await service.GetEnumItemsAsync(enumId, ct));

            api.MapPost("/basic-enums/items/create", async ([FromBody] BasicEnumItem item, IOntologyManageService service, CancellationToken ct) =>
                await service.CreateEnumItemAsync(item, ct));

            api.MapPut("/basic-enums/items/update", async ([FromBody] BasicEnumItem item, IOntologyManageService service, CancellationToken ct) =>
                await service.UpdateEnumItemAsync(item, ct));

            api.MapDelete("/basic-enums/items/delete/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteEnumItemAsync(id, ct));

            return app;
        }
    }
}
