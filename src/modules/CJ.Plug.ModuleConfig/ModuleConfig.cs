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

            // ── P5.6（D-P5-3=B 保留代码、摘除入口）：四模块 UI 注册下线 ──────────
            // 只摘「UI 侧」注册；②DI 侧（ConfigModuleApiServices）与 ③端点侧
            // （ConfigModuleApis）**保留不动**，源文件/csproj 一个不删。
            //
            // 机制：FrameworkRoutes 的 AdditionalAssemblies 由 IEnumerable<IModule> 反推
            //   ⇒ 不注册 IModule，该模块的 @page 路由、菜单项、AppBar 项**同时消失**，
            //     无需逐页处理，也无需改任何业务代码。
            //
            // 摘除收益（P5.4 实测登记项一并消掉）：
            //   ① 旧「系统管理/授权管理/审计管理」菜单与新三员同名菜单并存的口径双轨 → 消除；
            //   ② AppBar 的「欢迎：⎋」假登出按钮（LoginModule 经 AddAppBarItem<LoginState> 挂载，
            //      点击只清旧 localStorage + 跳 /login，不清三员 cookie、不撤销会话）→ 消失；
            //   ③ 「欢迎：」后用户名恒空（取旧 localStorage key，三员不写）→ 消失。
            //
            // ⚠ 回滚成本 = 取消下面 4 行注释 + 重编译。保留期一个版本周期。
            //services.AddUserManagePageModuleServices();
            services.AddMCPToolsModuleServices();
            services.AddSkillsModuleServices();
            services.AddKnowledgeModuleServices();

            // 授权管理模块 —— P5.6 摘除 UI 入口（三员 /three-role/auth 接管）
            //services.AddAuthModulePageServices();

            // 审计管理模块 —— P5.6 摘除 UI 入口（三员 /three-role/audit 接管）
            //services.AddAuditModulePageServices();

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

            // 登录模块 —— P5.6 摘除 UI 入口（三员 /three-role/login 接管）
            //services.AddLoginModulePageServices();

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
