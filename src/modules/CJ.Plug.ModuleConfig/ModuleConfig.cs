using CJ.Plug.AuthApi;
using CJ.Plug.AuthUI;
using CJ.Plug.AuditApi;
using CJ.Plug.AuditUI;
using CJ.Plug.GuacamoleApi;
using CJ.Plug.GuacamoleUI;
using CJ.Plug.HomePage;
using CJ.Plug.Login;
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
            services.AddUserManagePageModuleServices();
            services.AddMCPToolsModuleServices();
            services.AddSkillsModuleServices();
            services.AddKnowledgeModuleServices();

            // 授权管理模块
            services.AddAuthModulePageServices();

            // 审计管理模块
            services.AddAuditModulePageServices();

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

            //放在最后，因为用户名需要放在最后
            services.AddLoginModulePageServices();

            return services;
        }

        //配置各模块Api所需服务注入
        public static IServiceCollection ConfigModuleApiServices(this IServiceCollection services)
        {
            // 注册基础服务（泛型服务）
            services.AddScoped(typeof(IBaseRepositoryService<,>), typeof(BaseRepositoryService<,>));


            //services.AddLoginModuleApiServices();
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
            services.AddUserManageModuleApiServices();
            services.AddMCPToolsApiServices();
            services.AddSkillsApiServices();
            services.AddKnowledgeApiServices();

            // 授权管理模块API
            services.AddAuthModuleApiServices();

            // 审计管理模块API
            services.AddAuditModuleApiServices();

            // Guacamole 远程桌面模块API
            services.AddGuacamoleApiServices();

            // 模型管理模块API
            services.AddOntologyManageModuleApiServices();

            // 许可证模块API
            services.AddLicenseModuleApiServices();

            // LLM 配置模块API
            services.AddLlmConfigModuleApiServices();

            return services;
        }

        //配置各模块Api注入
        public static IApplicationBuilder ConfigModuleApis(this IApplicationBuilder app)
        {
            app.AddLoginModuleApi();
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
            app.AddUserManageModuleApi();
            app.AddMCPToolsApi();
            app.AddSkillsApi();
            app.AddKnowledgeApi();

            // 授权管理模块API
            app.AddAuthModuleApi();

            // 审计管理模块API
            app.AddAuditModuleApi();

            // Guacamole 远程桌面模块API
            app.AddGuacamoleApi();

            // 模型管理模块API
            app.AddOntologyManageModuleApi();

            // 许可证模块API
            app.AddLicenseModuleApi();

            // LLM 配置模块API
            app.AddLlmConfigModuleApi();

            return app;
        }

    }
}
