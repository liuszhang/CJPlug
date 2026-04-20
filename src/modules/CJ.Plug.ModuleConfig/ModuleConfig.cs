using CJ.Plug.HomePage;
using CJ.Plug.Login;
using CJ.Plug.LoginApiClient.ApiClients;
using CJ.Plug.LoginApis.Apis;
using CJ.Plug.LoginApis.Contracts;
using CJ.Plug.LoginApis.Services;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Services;
using CJ.Plug.MCPToolsManage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            services.AddStationAndToolModuleServices();
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
            services.AddStationAndToolApiServices();
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

            return services;
        }

        //配置各模块Api注入
        public static IApplicationBuilder ConfigModuleApis(this IApplicationBuilder app)
        {
            app.AddLoginModuleApi();
            app.AddProcessManageModuleApi();
            app.AddJobManageModuleApi();
            app.AddStationAndToolApi();
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

            return app;
        }

    }
}
