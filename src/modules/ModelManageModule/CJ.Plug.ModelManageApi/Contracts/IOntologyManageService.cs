using CJ.Plug.ModelManageModel.Models;

namespace CJ.Plug.ModelManageApi.Contracts
{
    public interface IOntologyManageService
    {
        // 本体 CRUD
        Task<IEnumerable<Ontology>> GetAllOntologiesAsync(CancellationToken ct = default);
        Task<IEnumerable<Ontology>> GetOntologyTreeAsync(CancellationToken ct = default);
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

        // 属性约束 CRUD
        Task<IEnumerable<PropertyConstraint>> GetConstraintsByPropertyIdAsync(int propertyId, CancellationToken ct = default);
        Task<PropertyConstraint?> CreateConstraintAsync(PropertyConstraint constraint, CancellationToken ct = default);
        Task<PropertyConstraint?> UpdateConstraintAsync(PropertyConstraint constraint, CancellationToken ct = default);
        Task<bool> DeleteConstraintAsync(int id, CancellationToken ct = default);

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

        // SecurityLevelAccess CRUD
        Task<IEnumerable<SecurityLevelAccess>> GetSecurityLevelAccessByPersonnelAsync(int personnelLevelItemId, CancellationToken ct = default);
        Task<bool> UpdateSecurityLevelAccessAsync(int personnelLevelItemId, List<int> dataLevelItemIds, CancellationToken ct = default);
        Task<bool> DeleteSecurityLevelAccessAsync(int personnelLevelItemId, CancellationToken ct = default);

        // ========== M4 场景 CRUD ==========
        Task<IEnumerable<Scenario>> GetAllScenariosAsync(CancellationToken ct = default);
        Task<Scenario?> GetScenarioByIdAsync(int id, CancellationToken ct = default);
        Task<Scenario?> CreateScenarioAsync(Scenario scenario, CancellationToken ct = default);
        Task<Scenario?> UpdateScenarioAsync(Scenario scenario, CancellationToken ct = default);
        Task<bool> DeleteScenarioAsync(int id, CancellationToken ct = default);

        // ========== M5 主体 CRUD ==========
        Task<IEnumerable<Subject>> GetAllSubjectsAsync(CancellationToken ct = default);
        Task<Subject?> GetSubjectByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<Subject>> GetChildrenByParentIdAsync(int parentId, CancellationToken ct = default);
        Task<Subject?> CreateSubjectAsync(Subject subject, CancellationToken ct = default);
        Task<Subject?> UpdateSubjectAsync(Subject subject, CancellationToken ct = default);
        Task<bool> DeleteSubjectAsync(int id, CancellationToken ct = default);

        // ========== M5 权限 CRUD ==========
        Task<IEnumerable<Permission>> GetAllPermissionsAsync(CancellationToken ct = default);
        Task<Permission?> GetPermissionByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<Permission>> GetPermissionsBySubjectIdAsync(int subjectId, CancellationToken ct = default);
        Task<Permission?> CreatePermissionAsync(Permission permission, CancellationToken ct = default);
        Task<Permission?> UpdatePermissionAsync(Permission permission, CancellationToken ct = default);
        Task<bool> DeletePermissionAsync(int id, CancellationToken ct = default);

        // ========== M5.5 外部系统 CRUD ==========
        Task<IEnumerable<ExternalSystem>> GetAllExternalSystemsAsync(CancellationToken ct = default);
        Task<ExternalSystem?> GetExternalSystemByIdAsync(int id, CancellationToken ct = default);
        Task<ExternalSystem?> CreateExternalSystemAsync(ExternalSystem externalSystem, CancellationToken ct = default);
        Task<ExternalSystem?> UpdateExternalSystemAsync(ExternalSystem externalSystem, CancellationToken ct = default);
        Task<bool> DeleteExternalSystemAsync(int id, CancellationToken ct = default);

        // ========== M5.5 接口契约 CRUD ==========
        Task<IEnumerable<InterfaceContract>> GetAllInterfaceContractsAsync(CancellationToken ct = default);
        Task<InterfaceContract?> GetInterfaceContractByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<InterfaceContract>> GetInterfaceContractsByExternalSystemIdAsync(int externalSystemId, CancellationToken ct = default);
        Task<InterfaceContract?> CreateInterfaceContractAsync(InterfaceContract interfaceContract, CancellationToken ct = default);
        Task<InterfaceContract?> UpdateInterfaceContractAsync(InterfaceContract interfaceContract, CancellationToken ct = default);
        Task<bool> DeleteInterfaceContractAsync(int id, CancellationToken ct = default);

        // ========== M6 异常类型 CRUD ==========
        Task<IEnumerable<ExceptionType>> GetAllExceptionTypesAsync(CancellationToken ct = default);
        Task<ExceptionType?> GetExceptionTypeByIdAsync(int id, CancellationToken ct = default);
        Task<ExceptionType?> CreateExceptionTypeAsync(ExceptionType exceptionType, CancellationToken ct = default);
        Task<ExceptionType?> UpdateExceptionTypeAsync(ExceptionType exceptionType, CancellationToken ct = default);
        Task<bool> DeleteExceptionTypeAsync(int id, CancellationToken ct = default);

        // ========== M6 补偿动作 CRUD ==========
        Task<IEnumerable<CompensationAction>> GetAllCompensationActionsAsync(CancellationToken ct = default);
        Task<CompensationAction?> GetCompensationActionByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<CompensationAction>> GetCompensationActionsByExceptionTypeIdAsync(int exceptionTypeId, CancellationToken ct = default);
        Task<CompensationAction?> CreateCompensationActionAsync(CompensationAction compensationAction, CancellationToken ct = default);
        Task<CompensationAction?> UpdateCompensationActionAsync(CompensationAction compensationAction, CancellationToken ct = default);
        Task<bool> DeleteCompensationActionAsync(int id, CancellationToken ct = default);

        // ========== M7 质量指标 CRUD ==========
        Task<IEnumerable<QualityMetric>> GetAllQualityMetricsAsync(CancellationToken ct = default);
        Task<QualityMetric?> GetQualityMetricByIdAsync(int id, CancellationToken ct = default);
        Task<QualityMetric?> CreateQualityMetricAsync(QualityMetric qualityMetric, CancellationToken ct = default);
        Task<QualityMetric?> UpdateQualityMetricAsync(QualityMetric qualityMetric, CancellationToken ct = default);
        Task<bool> DeleteQualityMetricAsync(int id, CancellationToken ct = default);

        // ========== M7 告警规则 CRUD ==========
        Task<IEnumerable<AlertRule>> GetAllAlertRulesAsync(CancellationToken ct = default);
        Task<AlertRule?> GetAlertRuleByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<AlertRule>> GetAlertRulesByQualityMetricIdAsync(int qualityMetricId, CancellationToken ct = default);
        Task<AlertRule?> CreateAlertRuleAsync(AlertRule alertRule, CancellationToken ct = default);
        Task<AlertRule?> UpdateAlertRuleAsync(AlertRule alertRule, CancellationToken ct = default);
        Task<bool> DeleteAlertRuleAsync(int id, CancellationToken ct = default);

        // ========== M7 改进措施 CRUD ==========
        Task<IEnumerable<ImprovementAction>> GetAllImprovementActionsAsync(CancellationToken ct = default);
        Task<ImprovementAction?> GetImprovementActionByIdAsync(int id, CancellationToken ct = default);
        Task<IEnumerable<ImprovementAction>> GetImprovementActionsByQualityMetricIdAsync(int qualityMetricId, CancellationToken ct = default);
        Task<ImprovementAction?> CreateImprovementActionAsync(ImprovementAction improvementAction, CancellationToken ct = default);
        Task<ImprovementAction?> UpdateImprovementActionAsync(ImprovementAction improvementAction, CancellationToken ct = default);
        Task<bool> DeleteImprovementActionAsync(int id, CancellationToken ct = default);

    }
}
