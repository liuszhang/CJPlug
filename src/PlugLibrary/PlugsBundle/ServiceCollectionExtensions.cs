using AiAgentPlug.Extensions;
using CJ.Plug.PlugBaseCore;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Services;
using CMDPlug.Extensions;
using CommonToolExecute.Extensions;
using DocumentFormat.OpenXml.Drawing.Charts;
using Elsa.Studio.Agents.UI.Pages;
using ExcelPlug.Extensions;
using FileDownloadPlug.Extensions;
using ForPlug.Extensions;
using IfPlug.Extensions;
using JavaScriptPlug.Extensions;
using MatlabPlug.Extensions;
using MCDataGetPlug.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NXPlug.Extensions;
using NXPlug_AIModel.Extensions;
using PlugsBundle.Models;
using PlugsBundle.SystemInitTools;
using PlugsBundle.SystemInitTools.NXGetParameters;
using PythonPlug.Extensions;
using RESTPlug.Extensions;
using SendHttpRequestPlug.Extensions;
using System.Reflection;
using TextReaderPlug.Extensions;
using TextWriterPlug.Extensions;
using WordPlug.Extensions;
using WordReadPlug.Extensions;
using LLMPlug.Extensions;
using PPTPlug.Extensions;
using AndPlug.Extensions;
using CalculatorPlug.Extensions;
using CSharpPlug.Extensions;
using DllLoaderPlug;
using JavaPlug.Extensions;
using StlViewerPlug.Extensions;
using PatranPlug.Extensions;
using PausePlug.Extensions;


namespace PlugsBundle
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 添加前端所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddPlugsBundle(this IServiceCollection services)
        {
            services.AddScoped<IPlugSettingDialogShow, PlugSettingDialogCoreShow>();
            services.AddScoped<IToolExecuteService, ToolExecuteService>();
            services.AddSingleton<ToolDownloadGuard>();
            services.AddSingleton<ResilientDownloader>();

            services.AddPatran();
            services.AddNX();
            services.AddPython();
            services.AddCMD();
            services.AddREST();
            services.AddTextReader();
            services.AddTextWriter();
            services.AddJavaScript();
            //services.AddMCDataGet();
            services.AddMatlab();
            services.AddWord();
            services.AddWordRead();
            services.AddLLMPlug();
            services.AddPPT();
            services.AddAiAgent();
            services.AddNXAIModel();
            services.AddSendHttpRequest();
            //services.AddWhile();
            services.AddExcel();
            services.AddFileDownload();
            services.AddCommonToolExecute();
            services.AddFor();
            services.AddIf();
            services.AddAnd();
            services.AddCSharp();
            services.AddJava();
            services.AddCalculator();
            services.AddDllLoaderPage();
            services.AddStlViewer();
            services.AddPause();



            //系统初始化工具服务
            services.AddScoped<IPlugCommonSettingContent, WordToPdf>();
            services.AddScoped<IPlugCommonSettingContent, InsertDataToWord>();
            //services.AddScoped<IPlugCommonSettingContent, NXGetParameters>();
            //services.AddScoped<IPlugCommonSettingContent, NXSetParameters>();
            //services.AddScoped<IPlugCommonSettingContent, NXToStl>();

            return services;
        }

        /// <summary>
        /// 添加API执行时所需依赖注入
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddPlugsExecutebundle(this IServiceCollection services)
        {
            services.AddScoped<IPlugExecuteHandlerService, PlugExecuteHandlerService>();
            services.AddScoped<IToolExecuteService, ToolExecuteService>();
            services.AddSingleton<ToolDownloadGuard>();
            services.AddSingleton<ResilientDownloader>();


            services.AddPatranExecute();
            services.AddNXExecute();
            services.AddPythonExecute();
            services.AddCMDExecute();
            services.AddSendHttpRequestExecute();
            services.AddRESTExecute();
            services.AddTextReaderExecute();
            services.AddTextWriterExecute();
            services.AddJavaScriptExecute();
            services.AddMCDataGetExecute();
            services.AddMatlabExecute();
            services.AddWordExecute();
            services.AddWordReadExecute();
            services.AddLLMPlugExecute();
            services.AddPPTExecute();
            services.AddNXAIModelExecute();
            //services.AddWhileExecute();
            services.AddExcelExecute();
            services.AddFileDownloadExecute();
            services.AddCommonToolExecuteExecute();
            services.AddForExecute();
            services.AddIfExecute();
            services.AddAndExecute();
            services.AddCSharpExecute();
            services.AddJavaExecute();
            services.AddCalculatorExecute();
            services.AddDllLoaderExecute();
            services.AddStlViewerExecute();
            services.AddPauseExecute();

            return services;
        }


        /// <summary>
        /// 通过XML配置文件动态加载和注册插头
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static IServiceCollection AddXmlConfiguredServices(this IServiceCollection services)
        {
            

            (var config, var basePath) = GetConfigInfo();

            foreach (var plugConfig in config.Plugs)
            {
                try
                {
                    // 构建完整的DLL路径
                    var dllPath = Path.GetFullPath(
                        Path.Combine(basePath, plugConfig.DllPath)
                    );

                    // 检查文件是否存在
                    if (!File.Exists(dllPath))
                    {
                        Console.WriteLine($"警告: DLL文件不存在 - {dllPath}");
                        continue;
                    }

                    // 加载程序集
                    var assembly = Assembly.LoadFrom(dllPath);

                    // 查找所有实现IService接口的公共类
                    var serviceTypes = assembly.GetTypes()
                        .Where(t => typeof(IPlugCommonSettingContent).IsAssignableFrom(t)
                                 && !t.IsAbstract
                                 && t.IsPublic);

                    // 注册服务
                    foreach (var serviceType in serviceTypes)
                    {
                        services.AddScoped(typeof(IPlugCommonSettingContent),serviceType);
                        Console.WriteLine($"service added: {serviceType.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"错误: 处理插件 {plugConfig.Name} 失败 - {ex.Message}");
                }
            }

            return services;
        }


        /// <summary>
        /// 通过XML配置文件动态加载和注册插头(执行服务)
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        public static IServiceCollection AddXmlConfiguredExecuteServices(this IServiceCollection services)
        {
            (var config, var basePath) = GetConfigInfo();

            foreach (var plugConfig in config.Plugs)
            {
                try
                {
                    // 构建完整的DLL路径
                    var dllPath = Path.GetFullPath(
                        Path.Combine(basePath, plugConfig.DllPath)
                    );

                    // 检查文件是否存在
                    if (!File.Exists(dllPath))
                    {
                        Console.WriteLine($"警告: DLL文件不存在 - {dllPath}");
                        continue;
                    }

                    // 加载程序集
                    var assembly = Assembly.LoadFrom(dllPath);

                    // 查找所有实现IService接口的公共类
                    var serviceTypes = assembly.GetTypes()
                        .Where(t =>
                                (typeof(IPlugCommonExecute).IsAssignableFrom(t) || typeof(DefaultPlugExecuteService).IsAssignableFrom(t))
                                 && !t.IsAbstract
                                 && t.IsPublic);

                    // 注册服务
                    foreach (var serviceType in serviceTypes)
                    {
                        services.AddScoped(typeof(IPlugCommonExecute), serviceType);
                        Console.WriteLine($"service added: {serviceType.FullName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"错误: 处理插件 {plugConfig.Name} 失败 - {ex.Message}");
                }
            }

            return services;
        }


        /// <summary>
        /// 处理配置文件路径和解析
        /// </summary>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException"></exception>
        private static (PlugsConfig config,string basePath) GetConfigInfo()
        {
            //var configFolderPath= Path.Combine(
            //    "C:/tmp",
            //    "PlugConfig"
            //);
            // 固定配置文件路径：/PlugConfig/UserPlugs.xml
            var configFolderPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "../../../PlugConfig"
            );

            //Console.WriteLine($"Plug Config Folder is:{configFolderPath}");

            var configFilePath = Path.Combine(
                configFolderPath,
                "UserPlugs.xml"
            );

            // 检查配置文件是否存在
            if (!File.Exists(configFilePath))
            {
                throw new FileNotFoundException($"未找到配置文件: {configFilePath}");
            }

            //Console.WriteLine($"find config file path:{configFilePath}");

            // 解析XML配置
            var config = XmlConfigParser.Parse(configFilePath);

            // 构建完整的基础路径
            //var basePath = Path.GetFullPath(
            //    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlugConfig", config.BasePath)
            //);
            var basePath = Path.GetFullPath(
                Path.Combine(configFolderPath, config.BasePath)
            );
            Console.WriteLine($"find base path:{basePath}");

            return (config,basePath);
        }
    }
}
