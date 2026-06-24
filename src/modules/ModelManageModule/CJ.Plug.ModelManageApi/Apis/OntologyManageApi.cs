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

            api.MapGet("/tree", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetOntologyTreeAsync(ct));

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

            // SecurityLevelAccess CRUD
            api.MapGet("/security-level-access/{personnelLevelItemId:int}", async (int personnelLevelItemId, IOntologyManageService service, CancellationToken ct) =>
                await service.GetSecurityLevelAccessByPersonnelAsync(personnelLevelItemId, ct));

            api.MapPut("/security-level-access/{personnelLevelItemId:int}", async (int personnelLevelItemId, [FromBody] List<int> dataLevelItemIds, IOntologyManageService service, CancellationToken ct) =>
                await service.UpdateSecurityLevelAccessAsync(personnelLevelItemId, dataLevelItemIds, ct));

            api.MapDelete("/security-level-access/{personnelLevelItemId:int}", async (int personnelLevelItemId, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteSecurityLevelAccessAsync(personnelLevelItemId, ct));

            // ========== M4 场景 CRUD ==========
            api.MapGet("/scenarios", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllScenariosAsync(ct));

            api.MapGet("/scenarios/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.GetScenarioByIdAsync(id, ct));

            api.MapPost("/scenarios", async ([FromBody] Scenario scenario, IOntologyManageService service, CancellationToken ct) =>
                await service.CreateScenarioAsync(scenario, ct));

            api.MapPut("/scenarios/{id}", async (int id, [FromBody] Scenario scenario, IOntologyManageService service, CancellationToken ct) =>
            {
                scenario.Id = id;
                return await service.UpdateScenarioAsync(scenario, ct);
            });

            api.MapDelete("/scenarios/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteScenarioAsync(id, ct));

            // ========== M5 主体 CRUD ==========
            api.MapGet("/subjects", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllSubjectsAsync(ct));

            api.MapGet("/subjects/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.GetSubjectByIdAsync(id, ct));

            api.MapGet("/subjects/{parentId}/children", async (int parentId, IOntologyManageService service, CancellationToken ct) =>
                await service.GetChildrenByParentIdAsync(parentId, ct));

            api.MapPost("/subjects", async ([FromBody] Subject subject, IOntologyManageService service, CancellationToken ct) =>
                await service.CreateSubjectAsync(subject, ct));

            api.MapPut("/subjects/{id}", async (int id, [FromBody] Subject subject, IOntologyManageService service, CancellationToken ct) =>
            {
                subject.Id = id;
                return await service.UpdateSubjectAsync(subject, ct);
            });

            api.MapDelete("/subjects/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteSubjectAsync(id, ct));

            // ========== M5 权限 CRUD ==========
            api.MapGet("/permissions", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllPermissionsAsync(ct));

            api.MapGet("/permissions/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.GetPermissionByIdAsync(id, ct));

            api.MapGet("/subjects/{subjectId}/permissions", async (int subjectId, IOntologyManageService service, CancellationToken ct) =>
                await service.GetPermissionsBySubjectIdAsync(subjectId, ct));

            api.MapPost("/permissions", async ([FromBody] Permission permission, IOntologyManageService service, CancellationToken ct) =>
                await service.CreatePermissionAsync(permission, ct));

            api.MapPut("/permissions/{id}", async (int id, [FromBody] Permission permission, IOntologyManageService service, CancellationToken ct) =>
            {
                permission.Id = id;
                return await service.UpdatePermissionAsync(permission, ct);
            });

            api.MapDelete("/permissions/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeletePermissionAsync(id, ct));

            // ========== M5.5 外部系统 CRUD ==========
            api.MapGet("/external-systems", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllExternalSystemsAsync(ct));

            api.MapGet("/external-systems/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.GetExternalSystemByIdAsync(id, ct));

            api.MapPost("/external-systems", async ([FromBody] ExternalSystem externalSystem, IOntologyManageService service, CancellationToken ct) =>
                await service.CreateExternalSystemAsync(externalSystem, ct));

            api.MapPut("/external-systems/{id}", async (int id, [FromBody] ExternalSystem externalSystem, IOntologyManageService service, CancellationToken ct) =>
            {
                externalSystem.Id = id;
                return await service.UpdateExternalSystemAsync(externalSystem, ct);
            });

            api.MapDelete("/external-systems/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteExternalSystemAsync(id, ct));

            // ========== M5.5 接口契约 CRUD ==========
            api.MapGet("/interface-contracts", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllInterfaceContractsAsync(ct));

            api.MapGet("/interface-contracts/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.GetInterfaceContractByIdAsync(id, ct));

            api.MapGet("/external-systems/{externalSystemId}/contracts", async (int externalSystemId, IOntologyManageService service, CancellationToken ct) =>
                await service.GetInterfaceContractsByExternalSystemIdAsync(externalSystemId, ct));

            api.MapPost("/interface-contracts", async ([FromBody] InterfaceContract interfaceContract, IOntologyManageService service, CancellationToken ct) =>
                await service.CreateInterfaceContractAsync(interfaceContract, ct));

            api.MapPut("/interface-contracts/{id}", async (int id, [FromBody] InterfaceContract interfaceContract, IOntologyManageService service, CancellationToken ct) =>
            {
                interfaceContract.Id = id;
                return await service.UpdateInterfaceContractAsync(interfaceContract, ct);
            });

            api.MapDelete("/interface-contracts/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteInterfaceContractAsync(id, ct));

            // ========== M6 异常类型 CRUD ==========
            api.MapGet("/exception-types", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllExceptionTypesAsync(ct));

            api.MapGet("/exception-types/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.GetExceptionTypeByIdAsync(id, ct));

            api.MapPost("/exception-types", async ([FromBody] ExceptionType exceptionType, IOntologyManageService service, CancellationToken ct) =>
                await service.CreateExceptionTypeAsync(exceptionType, ct));

            api.MapPut("/exception-types/{id}", async (int id, [FromBody] ExceptionType exceptionType, IOntologyManageService service, CancellationToken ct) =>
            {
                exceptionType.Id = id;
                return await service.UpdateExceptionTypeAsync(exceptionType, ct);
            });

            api.MapDelete("/exception-types/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteExceptionTypeAsync(id, ct));

            // ========== M6 补偿动作 CRUD ==========
            api.MapGet("/compensation-actions", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllCompensationActionsAsync(ct));

            api.MapGet("/compensation-actions/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.GetCompensationActionByIdAsync(id, ct));

            api.MapGet("/exception-types/{exceptionTypeId}/compensation-actions", async (int exceptionTypeId, IOntologyManageService service, CancellationToken ct) =>
                await service.GetCompensationActionsByExceptionTypeIdAsync(exceptionTypeId, ct));

            api.MapPost("/compensation-actions", async ([FromBody] CompensationAction compensationAction, IOntologyManageService service, CancellationToken ct) =>
                await service.CreateCompensationActionAsync(compensationAction, ct));

            api.MapPut("/compensation-actions/{id}", async (int id, [FromBody] CompensationAction compensationAction, IOntologyManageService service, CancellationToken ct) =>
            {
                compensationAction.Id = id;
                return await service.UpdateCompensationActionAsync(compensationAction, ct);
            });

            api.MapDelete("/compensation-actions/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteCompensationActionAsync(id, ct));

            // ========== M7 质量指标 CRUD ==========
            api.MapGet("/quality-metrics", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllQualityMetricsAsync(ct));

            api.MapGet("/quality-metrics/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.GetQualityMetricByIdAsync(id, ct));

            api.MapPost("/quality-metrics", async ([FromBody] QualityMetric qualityMetric, IOntologyManageService service, CancellationToken ct) =>
                await service.CreateQualityMetricAsync(qualityMetric, ct));

            api.MapPut("/quality-metrics/{id}", async (int id, [FromBody] QualityMetric qualityMetric, IOntologyManageService service, CancellationToken ct) =>
            {
                qualityMetric.Id = id;
                return await service.UpdateQualityMetricAsync(qualityMetric, ct);
            });

            api.MapDelete("/quality-metrics/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteQualityMetricAsync(id, ct));

            // ========== M7 告警规则 CRUD ==========
            api.MapGet("/alert-rules", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllAlertRulesAsync(ct));

            api.MapGet("/alert-rules/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.GetAlertRuleByIdAsync(id, ct));

            api.MapGet("/quality-metrics/{qualityMetricId}/alert-rules", async (int qualityMetricId, IOntologyManageService service, CancellationToken ct) =>
                await service.GetAlertRulesByQualityMetricIdAsync(qualityMetricId, ct));

            api.MapPost("/alert-rules", async ([FromBody] AlertRule alertRule, IOntologyManageService service, CancellationToken ct) =>
                await service.CreateAlertRuleAsync(alertRule, ct));

            api.MapPut("/alert-rules/{id}", async (int id, [FromBody] AlertRule alertRule, IOntologyManageService service, CancellationToken ct) =>
            {
                alertRule.Id = id;
                return await service.UpdateAlertRuleAsync(alertRule, ct);
            });

            api.MapDelete("/alert-rules/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteAlertRuleAsync(id, ct));

            // ========== M7 改进措施 CRUD ==========
            api.MapGet("/improvement-actions", async (IOntologyManageService service, CancellationToken ct) =>
                await service.GetAllImprovementActionsAsync(ct));

            api.MapGet("/improvement-actions/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.GetImprovementActionByIdAsync(id, ct));

            api.MapGet("/quality-metrics/{qualityMetricId}/improvement-actions", async (int qualityMetricId, IOntologyManageService service, CancellationToken ct) =>
                await service.GetImprovementActionsByQualityMetricIdAsync(qualityMetricId, ct));

            api.MapPost("/improvement-actions", async ([FromBody] ImprovementAction improvementAction, IOntologyManageService service, CancellationToken ct) =>
                await service.CreateImprovementActionAsync(improvementAction, ct));

            api.MapPut("/improvement-actions/{id}", async (int id, [FromBody] ImprovementAction improvementAction, IOntologyManageService service, CancellationToken ct) =>
            {
                improvementAction.Id = id;
                return await service.UpdateImprovementActionAsync(improvementAction, ct);
            });

            api.MapDelete("/improvement-actions/{id}", async (int id, IOntologyManageService service, CancellationToken ct) =>
                await service.DeleteImprovementActionAsync(id, ct));

            return app;
        }
    }
}
