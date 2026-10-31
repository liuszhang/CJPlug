using CJ.Plug.AuditApi;
using CJ.Plug.GuacamoleApi;
using CJ.Plug.GuacamoleUI;
using CJ.Plug.HomePage;
using CJ.Plug.LlmConfig;
using CJ.Plug.LicenseApi;
using CJ.Plug.LicenseUI;
using CJ.Plug.LlmConfigApi;
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

            // Guacamole 远程桌面模块
            services.AddGuacamoleUIPageServices();

            // LLM 配置模块
            services.AddLlmConfigPageModuleServices();

            // ── P7.1a（2026-10-30）：CJPlug 私有系统配置模块已退役 ──
            // 页面写侧早已由 CJCore 通用配置页 /system-config 接管（P7.1）；
            // 本批把剩余的读侧（ToolExecuteService 经 /api/systemconfig/*）也换到
            // IRemoteViewModeProvider（宿主实现见 HostWebServer/SystemConfig/CJCoreRemoteViewModeProvider）。
            // 删掉的是整个 SystemConfigModule 目录（页面 / Api / ApiClient / Model 四个项目）与 SystemConfigItems 表。

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

            // 审计管理模块API（端点保留：业务侧 299 处 AuditLogHelper 调用写 /api/audit/log）
            services.AddAuditModuleApiServices();

            // Guacamole 远程桌面模块API
            services.AddGuacamoleApiServices();

            // 许可证模块API
            services.AddLicenseModuleApiServices();

            // LLM 配置模块API
            services.AddLlmConfigModuleApiServices();

            return services;
        }

        /// <summary>
        /// 只注册业务模块的 <c>IModuleBusinessDbConfig</c>（建模用），<b>不</b>注册端点/API 客户端/UI 服务。
        /// </summary>
        /// <remarks>
        /// <b>为什么单独拆一个方法</b>（2026-10-30）：HostWebServer（5066，Elsa 引擎宿主）与 ApiServer（8687）
        /// 共用同一个 <c>main.db</c> 与同一份 <c>__CjModelSnapshot</c>。CJCore 自动迁移按「模型 vs 实际库」diff，
        /// 因此两个宿主的 <c>DataDbContext</c> <b>模型集必须一致</b>，否则后启动的「小模型」宿主会判定
        /// 「模型整体回退」要删表 ⇒ fail-fast ⇒ 5066 起不来。
        /// 但直接调 <see cref="ConfigModuleApiServices"/> 会把各模块的 API 客户端（LicenseApiClient 等）也带进
        /// 5066，页面组件随即通过 HttpClient <b>自调用 localhost:5066</b>，连接不释放而累积（实测 17677 条
        /// ESTABLISHED）⇒ WSAENOBUFS(10055) ⇒ 服务事实上不可用。建模只需要 DbConfig，故只注册这一部分。
        /// </remarks>
        public static IServiceCollection ConfigModuleDbConfigServices(this IServiceCollection services)
        {
            services.AddSingleton<IModuleBusinessDbConfig, CJ.Plug.AuditApi.DbContext.AuditModuleDbConfig>();
            services.AddSingleton<IModuleBusinessDbConfig, CJ.Plug.FileManageApi.DbContext.FileModuleDbConfig>();
            services.AddSingleton<IModuleBusinessDbConfig, CJ.Plug.JobManageApi.DbContext.JobModuleDbConfig>();
            services.AddSingleton<IModuleBusinessDbConfig, CJ.Plug.LicenseApi.DbContext.LicenseModuleDbConfig>();
            services.AddSingleton<IModuleBusinessDbConfig, CJ.Plug.LlmConfigModel.DbContext.LlmConfigDbConfig>();
            services.AddSingleton<IModuleBusinessDbConfig, CJ.Plug.MCPToolsManageApi.DbContext.MCPToolsManageDbConfig>();
            services.AddSingleton<IModuleBusinessDbConfig, CJ.Plug.PDZModels.DbContext.PDZModuleDbConfig>();
            services.AddSingleton<IModuleBusinessDbConfig, PlugMarketModuleDbConfig>();   // 该类型无命名空间（全局）
            services.AddSingleton<IModuleBusinessDbConfig, CJ.Plug.ProcessManageApi.DbContext.ProcessManageModuleDbConfig>();
            services.AddSingleton<IModuleBusinessDbConfig, CJ.Plug.RelationApi.DbContext.RelationModuleDbConfig>();
            services.AddSingleton<IModuleBusinessDbConfig, CJ.Plug.SkillsManageApi.SkillsManageDbConfig>();
            services.AddSingleton<IModuleBusinessDbConfig, CJ.Plug.StationManageApi.DbContext.StationManageDbConfig>();
            services.AddSingleton<IModuleBusinessDbConfig, CJ.Plug.ToolActionSettingApi.DbContext.TASModuleDbConfig>();
            services.AddSingleton<IModuleBusinessDbConfig, CJ.Plug.ToolResourceApi.DbContext.ToolResourceDbConfig>();

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

            // 审计管理模块API（端点保留，见 ConfigModuleApiServices 注释）
            app.AddAuditModuleApi();

            // Guacamole 远程桌面模块API
            app.AddGuacamoleApi();

            // 许可证模块API
            app.AddLicenseModuleApi();

            // LLM 配置模块API
            app.AddLlmConfigModuleApi();

            return app;
        }

    }
}
