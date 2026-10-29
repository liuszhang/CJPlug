using CJ.Plug.AuditApi;
using CJ.Plug.GuacamoleApi;
using CJ.Plug.GuacamoleUI;
using CJ.Plug.HomePage;
using CJ.Plug.KnowledgeManage;
using CJ.Plug.LlmConfig;
using CJ.Plug.LicenseApi;
using CJ.Plug.LicenseUI;
using CJ.Plug.LlmConfigApi;
using CJ.Plug.SystemConfig;
using CJ.Plug.ModelManage;
using CJ.Plug.ModelManageApi;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using CJ.Plug.SystemConfigApi;

namespace CJ.Plug.ModuleConfig
{
    public static class ModuleConfig
    {
        //配置各模块前端页面控件和Apiclient的服务注入
        public static IServiceCollection ConfigModulePageServices(this IServiceCollection services)
        {
            services.AddHomePageModuleServices();
            services.AddProcessManagePageModuleServices();
            services.AddJobManagePageModuleServices();
            services.AddTASPageModuleServices();
            services.AddStationManageModuleServices();
            services.AddToolResourceModuleServices();
            services.AddPlugMarketPageModuleServices();
            services.AddPlugExecutePageModuleServices();
            services.AddProcessEditPageModuleServices();
            services.AddPDZManagePageModuleServices();
            services.AddFileManagePageModuleServices();

            services.AddAIModuleServices();
            services.AddUnitTestPageModuleServices();

            services.AddRelationManagePageModuleServices();

            services.AddFMDModuleServices();

            // ── P5.6 终态：旧身份四模块（Login / UserManage / Auth / Audit UI）已物理删除 ──
            // 删除范围（13 个项目，git 可恢复）：
            //   LoginModule/{CJ.Plug.Login, LoginApis, LoginPages, LoginApiClient}
            //   UserManageModule/{CJ.Plug.UserManage, UserManageApi, UserManageApiClient, UserManageModels}
            //   AuthModule/{AuthApi, AuthUI, AuthApiClient, AuthModels}
            //   AuditModule/CJ.Plug.AuditUI
            // 保留：AuditApi（/api/audit/log 写入端点）+ AuditModels（AuditOperationType/AuditModule 枚举
            //       + CreateAuditLogRequest 契约）—— 业务侧 299 处 AuditLogHelper 调用依赖它，
            //       删掉会让每条业务操作打一条"审计日志记录失败"告警，成为新的日志刷屏源。
            //       审计数据迁移到三员审计库（main_audit.db）单独立项。
            // 接管方：CJCore.Modules.ThreeRoleAdmin（三员分权）+ PersonalCenter。
            services.AddMCPToolsModuleServices();
            services.AddSkillsModuleServices();
            services.AddKnowledgeModuleServices();

            // Guacamole 远程桌面模块
            services.AddGuacamoleUIPageServices();

            // 模型管理模块
            services.AddOntologyManagePageModuleServices();

            // LLM 配置模块
            services.AddLlmConfigPageModuleServices();

            // 系统配置模块
            services.AddSystemConfigPageModuleServices();
            //许可证管理模块
            services.AddLicenseModulePageServices();

            return services;
        }

        //配置各模块Api所需服务注入
        public static IServiceCollection ConfigModuleApiServices(this IServiceCollection services)
        {
            // 注册基础服务（泛型服务）
            services.AddScoped(typeof(IBaseRepositoryService<,>), typeof(BaseRepositoryService<,>));

            services.AddProcessManageModuleApiServices();
            services.AddJobManageModuleApiServices();
            services.AddStationManageApiServices();
            services.AddToolResourceApiServices();
            services.AddTASModuleApiServices();
            services.AddPDZManageModuleApiServices();
            services.AddPlugExecuteModuleApiServices();
            services.AddFileManageModuleApiServices();
            services.AddRelationManageModuleApiServices();
            services.AddPlugMarketModuleApiServices();
            services.AddToExternalModuleApiServices();
            services.AddMCPToolsApiServices();
            services.AddSkillsApiServices();
            services.AddKnowledgeApiServices();

            // 审计管理模块API（端点保留：业务侧 299 处 AuditLogHelper 调用写 /api/audit/log）
            services.AddAuditModuleApiServices();

            // Guacamole 远程桌面模块API
            services.AddGuacamoleApiServices();

            // 模型管理模块API
            services.AddOntologyManageModuleApiServices();

            // 系统配置模块API
            services.AddSystemConfigModuleApiServices();

            // 许可证模块API
            services.AddLicenseModuleApiServices();

            // LLM 配置模块API
            services.AddLlmConfigModuleApiServices();

            return services;
        }

        //配置各模块Api注入
        public static IApplicationBuilder ConfigModuleApis(this IApplicationBuilder app)
        {
            app.AddProcessManageModuleApi();
            app.AddJobManageModuleApi();
            app.AddStationManageApi();
            app.AddToolResourceApi();
            app.AddTASModuleApi();
            app.AddPDZManageModuleApi();
            app.AddPlugExecuteModuleApi();
            app.AddFileManageModuleApi();
            app.AddRelationManageModuleApi();
            app.AddPlugMarketModuleApi();
            app.AddToExternalModuleApi();
            app.AddMCPToolsApi();
            app.AddSkillsApi();
            app.AddKnowledgeApi();

            // 审计管理模块API（端点保留，见 ConfigModuleApiServices 注释）
            app.AddAuditModuleApi();

            // Guacamole 远程桌面模块API
            app.AddGuacamoleApi();

            // 模型管理模块API
            app.AddOntologyManageModuleApi();

            // 系统配置模块API
            app.AddSystemConfigModuleApi();

            // 许可证模块API
            app.AddLicenseModuleApi();

            // LLM 配置模块API
            app.AddLlmConfigModuleApi();

            return app;
        }

    }
}
