using CJ.Plug.ModelManageModel.Models;
using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Shared;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CJ.Plug.ModelManageApi.Services
{
    /// <summary>
    /// 本体管理模块种子数据提供者
    /// </summary>
    public class OntologyManageSeedDataProvider : ISeedDataProvider
    {
        public string Name => "本体管理模块种子数据";
        public int Order => 120;

        public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            var dbContext = serviceProvider.GetRequiredService<MainDbContext>();

            await SeedOntologyDefinitionsAsync(dbContext, cancellationToken);
            await SeedObjectInstancesAsync(dbContext, cancellationToken);
            await SeedBasicEnumsAsync(dbContext, cancellationToken);
        }

        #region 本体定义

        private static async Task SeedOntologyDefinitionsAsync(MainDbContext dbContext, CancellationToken cancellationToken)
        {
            var existing = await dbContext.Set<Ontology>()
                .FirstOrDefaultAsync(d => d.IsSystem, cancellationToken);
            if (existing != null)
            {
                Console.WriteLine("[SeedData] OntologyManage 种子数据已存在，跳过");
                return;
            }

            var now = new DateTime(2026, 5, 26, 0, 0, 0);

            // ---- 1. 文本文件 ----
            var textFile = new Ontology
            {
                Name = "文本文件", DisplayName = "文本文件", Code = "TextFile",
                Description = "管理文本文档类型，如需求文档、设计说明、会议纪要等。",
                IsEnabled = true, SortOrder = 1, IsSystem = true,
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            dbContext.Set<Ontology>().Add(textFile);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.Set<Property>().AddRange(
                new Property { OntologyId = textFile.Id, Name = "编码格式", Code = "Encoding",         PropertyType = "Select",   SortOrder = 1, DefaultValue = "UTF-8",  IsRequired = true,  SelectOptions = "[\"UTF-8\",\"GBK\",\"ASCII\"]" },
                new Property { OntologyId = textFile.Id, Name = "文件大小(KB)", Code = "FileSizeKB",   PropertyType = "Number",   SortOrder = 2, DefaultValue = "0",      IsRequired = false },
                new Property { OntologyId = textFile.Id, Name = "行数",      Code = "LineCount",        PropertyType = "Number",   SortOrder = 3, DefaultValue = "0",      IsRequired = false },
                new Property { OntologyId = textFile.Id, Name = "内容摘要",  Code = "ContentSummary",   PropertyType = "Text",     SortOrder = 4, DefaultValue = "",       IsRequired = false }
            );
            await dbContext.SaveChangesAsync(cancellationToken);
            Log.Information("[SeedData] 本体定义 {Name}（{Code}）已创建", textFile.Name, textFile.Code);

            // ---- 2. 三维模型 ----
            var threeD = new Ontology
            {
                Name = "三维模型", DisplayName = "三维模型", Code = "ThreeDModel",
                Description = "管理三维几何模型，包括网格模型、曲面模型及实体模型。",
                IsEnabled = true, SortOrder = 2, IsSystem = true,
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            dbContext.Set<Ontology>().Add(threeD);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.Set<Property>().AddRange(
                new Property { OntologyId = threeD.Id, Name = "顶点数",   Code = "VertexCount",   PropertyType = "Number", SortOrder = 1, DefaultValue = "0",   IsRequired = false },
                new Property { OntologyId = threeD.Id, Name = "面数",     Code = "FaceCount",     PropertyType = "Number", SortOrder = 2, DefaultValue = "0",   IsRequired = false },
                new Property { OntologyId = threeD.Id, Name = "材质类型", Code = "MaterialType",  PropertyType = "Text",   SortOrder = 3, DefaultValue = "",    IsRequired = false },
                new Property { OntologyId = threeD.Id, Name = "文件格式", Code = "FileFormat",    PropertyType = "Select", SortOrder = 4, DefaultValue = "OBJ", IsRequired = true,  SelectOptions = "[\"OBJ\",\"FBX\",\"GLB\",\"STL\"]" },
                new Property { OntologyId = threeD.Id, Name = "长度",     Code = "Length",        PropertyType = "Number", SortOrder = 5, DefaultValue = "0",   IsRequired = false },
                new Property { OntologyId = threeD.Id, Name = "宽度",     Code = "Width",         PropertyType = "Number", SortOrder = 6, DefaultValue = "0",   IsRequired = false },
                new Property { OntologyId = threeD.Id, Name = "高度",     Code = "Height",        PropertyType = "Number", SortOrder = 7, DefaultValue = "0",   IsRequired = false }
            );
            await dbContext.SaveChangesAsync(cancellationToken);
            Log.Information("[SeedData] 本体定义 {Name}（{Code}）已创建", threeD.Name, threeD.Code);

            // ---- 3. 二维图纸 ----
            var twoD = new Ontology
            {
                Name = "二维图纸", DisplayName = "二维图纸", Code = "TwoDDrawing",
                Description = "管理工程图纸、施工图、原理图等二维设计文档。",
                IsEnabled = true, SortOrder = 3, IsSystem = true,
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            dbContext.Set<Ontology>().Add(twoD);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.Set<Property>().AddRange(
                new Property { OntologyId = twoD.Id, Name = "图纸比例", Code = "DrawingScale",  PropertyType = "Text",   SortOrder = 1, DefaultValue = "1:1",  IsRequired = false },
                new Property { OntologyId = twoD.Id, Name = "图层数",   Code = "LayerCount",    PropertyType = "Number", SortOrder = 2, DefaultValue = "0",    IsRequired = false },
                new Property { OntologyId = twoD.Id, Name = "标注信息", Code = "AnnotationInfo",PropertyType = "Text",   SortOrder = 3, DefaultValue = "",     IsRequired = false },
                new Property { OntologyId = twoD.Id, Name = "图纸尺寸", Code = "DrawingSize",   PropertyType = "Select", SortOrder = 4, DefaultValue = "A4",   IsRequired = false, SelectOptions = "[\"A0\",\"A1\",\"A2\",\"A3\",\"A4\"]" },
                new Property { OntologyId = twoD.Id, Name = "文件格式", Code = "FileFormat",    PropertyType = "Select", SortOrder = 5, DefaultValue = "DWG",  IsRequired = true,  SelectOptions = "[\"DWG\",\"DXF\",\"PDF\"]" }
            );
            await dbContext.SaveChangesAsync(cancellationToken);
            Log.Information("[SeedData] 本体定义 {Name}（{Code}）已创建", twoD.Name, twoD.Code);

            // ---- 4. 仿真模型 ----
            var sim = new Ontology
            {
                Name = "仿真模型", DisplayName = "仿真模型", Code = "SimulationModel",
                Description = "管理仿真分析模型，涵盖结构、流体、热力学等多物理场仿真。",
                IsEnabled = true, SortOrder = 4, IsSystem = true,
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            dbContext.Set<Ontology>().Add(sim);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.Set<Property>().AddRange(
                new Property { OntologyId = sim.Id, Name = "仿真类型",   Code = "SimulationType", PropertyType = "Select",   SortOrder = 1, DefaultValue = "流体", IsRequired = true,  SelectOptions = "[\"流体\",\"结构\",\"热力学\",\"电磁\"]" },
                new Property { OntologyId = sim.Id, Name = "参数配置",   Code = "ParamConfig",    PropertyType = "JsonText", SortOrder = 2, DefaultValue = "{}",  IsRequired = false },
                new Property { OntologyId = sim.Id, Name = "输入变量数", Code = "InputCount",     PropertyType = "Number",   SortOrder = 3, DefaultValue = "0",   IsRequired = false },
                new Property { OntologyId = sim.Id, Name = "输出变量数", Code = "OutputCount",    PropertyType = "Number",   SortOrder = 4, DefaultValue = "0",   IsRequired = false },
                new Property { OntologyId = sim.Id, Name = "求解器类型", Code = "SolverType",     PropertyType = "Text",     SortOrder = 5, DefaultValue = "",    IsRequired = false }
            );
            await dbContext.SaveChangesAsync(cancellationToken);
            Log.Information("[SeedData] 本体定义 {Name}（{Code}）已创建", sim.Name, sim.Code);

            // ---- 5. 体系模型 ----
            var arch = new Ontology
            {
                Name = "体系模型", DisplayName = "体系模型", Code = "ArchitectureModel",
                Description = "管理系统体系架构模型，描述系统间层级关系、接口与集成方式。",
                IsEnabled = true, SortOrder = 5, IsSystem = true,
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            dbContext.Set<Ontology>().Add(arch);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.Set<Property>().AddRange(
                new Property { OntologyId = arch.Id, Name = "体系层级",   Code = "ArchLevel",      PropertyType = "Text",     SortOrder = 1, DefaultValue = "",   IsRequired = false },
                new Property { OntologyId = arch.Id, Name = "关联系统数", Code = "RelatedSysCount", PropertyType = "Number",   SortOrder = 2, DefaultValue = "0",  IsRequired = false },
                new Property { OntologyId = arch.Id, Name = "接口定义",   Code = "InterfaceDef",   PropertyType = "JsonText", SortOrder = 3, DefaultValue = "{}", IsRequired = false },
                new Property { OntologyId = arch.Id, Name = "版本号",     Code = "Version",        PropertyType = "Text",     SortOrder = 4, DefaultValue = "1.0",IsRequired = false },
                new Property { OntologyId = arch.Id, Name = "架构风格",   Code = "ArchStyle",      PropertyType = "Text",     SortOrder = 5, DefaultValue = "",   IsRequired = false }
            );
            await dbContext.SaveChangesAsync(cancellationToken);
            Log.Information("[SeedData] 本体定义 {Name}（{Code}）已创建", arch.Name, arch.Code);

            // ---- 6. 系统模型 ----
            var sys = new Ontology
            {
                Name = "系统模型", DisplayName = "系统模型", Code = "SystemModel",
                Description = "管理单个系统的模型定义，描述系统的组成、依赖与边界条件。",
                IsEnabled = true, SortOrder = 6, IsSystem = true,
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            dbContext.Set<Ontology>().Add(sys);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.Set<Property>().AddRange(
                new Property { OntologyId = sys.Id, Name = "子系统数", Code = "SubSysCount",    PropertyType = "Number",   SortOrder = 1, DefaultValue = "0",   IsRequired = false },
                new Property { OntologyId = sys.Id, Name = "依赖关系", Code = "Dependencies",   PropertyType = "JsonText", SortOrder = 2, DefaultValue = "{}",  IsRequired = false },
                new Property { OntologyId = sys.Id, Name = "边界条件", Code = "BoundaryCond",   PropertyType = "Text",     SortOrder = 3, DefaultValue = "",    IsRequired = false },
                new Property { OntologyId = sys.Id, Name = "版本号",   Code = "Version",        PropertyType = "Text",     SortOrder = 4, DefaultValue = "1.0", IsRequired = false },
                new Property { OntologyId = sys.Id, Name = "所属体系", Code = "BelongToArch",   PropertyType = "Text",     SortOrder = 5, DefaultValue = "",    IsRequired = false },
                new Property { OntologyId = sys.Id, Name = "集成方式", Code = "IntegrationMode",PropertyType = "Text",     SortOrder = 6, DefaultValue = "",    IsRequired = false }
            );
            await dbContext.SaveChangesAsync(cancellationToken);
            Log.Information("[SeedData] 本体定义 {Name}（{Code}）已创建", sys.Name, sys.Code);

            Console.WriteLine("[SeedData] 6 种本体定义及属性字段种子数据全部创建完成");
        }

        #endregion

        #region 对象实例

        private static async Task SeedObjectInstancesAsync(MainDbContext dbContext, CancellationToken cancellationToken)
        {
            var existing = await dbContext.Set<ObjectInstance>()
                .FirstOrDefaultAsync(cancellationToken);
            if (existing != null)
            {
                Console.WriteLine("[SeedData] OntologyManage 实例种子数据已存在，跳过");
                return;
            }

            var now = new DateTime(2026, 5, 26, 0, 0, 0);

            var textFile = await dbContext.Set<Ontology>().FirstAsync(d => d.Code == "TextFile", cancellationToken);
            var threeD   = await dbContext.Set<Ontology>().FirstAsync(d => d.Code == "ThreeDModel", cancellationToken);
            var twoD     = await dbContext.Set<Ontology>().FirstAsync(d => d.Code == "TwoDDrawing", cancellationToken);
            var sim      = await dbContext.Set<Ontology>().FirstAsync(d => d.Code == "SimulationModel", cancellationToken);
            var arch     = await dbContext.Set<Ontology>().FirstAsync(d => d.Code == "ArchitectureModel", cancellationToken);
            var sys      = await dbContext.Set<Ontology>().FirstAsync(d => d.Code == "SystemModel", cancellationToken);

            var instances = new List<ObjectInstance>
            {
                new() { OntologyId = textFile.Id, Name = "需求规格说明书", Description = "项目核心需求文档",
                    PropertiesJson = """{"Encoding":"UTF-8","FileSizeKB":256,"LineCount":3200,"ContentSummary":"描述系统功能需求、非功能需求及接口规范"}""",
                    CreatedAt = now, CreatedBy = "System" },
                new() { OntologyId = textFile.Id, Name = "会议纪要-20260520", Description = "第15次项目例会纪要",
                    PropertiesJson = """{"Encoding":"UTF-8","FileSizeKB":48,"LineCount":320,"ContentSummary":"讨论模块接口联调方案、里程碑进度及风险事项"}""",
                    CreatedAt = now, CreatedBy = "System" },
                new() { OntologyId = textFile.Id, Name = "API接口文档", Description = "RESTful API 接口说明",
                    PropertiesJson = """{"Encoding":"UTF-8","FileSizeKB":128,"LineCount":1800,"ContentSummary":"定义所有API端点、请求/响应格式、认证方式及错误码"}""",
                    CreatedAt = now, CreatedBy = "System" },

                new() { OntologyId = threeD.Id, Name = "主厂房结构模型", Description = "主厂房钢结构三维模型",
                    PropertiesJson = """{"VertexCount":125000,"FaceCount":98000,"MaterialType":"钢结构","FileFormat":"OBJ","Length":120,"Width":60,"Height":35}""",
                    CreatedAt = now, CreatedBy = "System" },
                new() { OntologyId = threeD.Id, Name = "管道系统模型", Description = "工艺管道三维布置模型",
                    PropertiesJson = """{"VertexCount":45000,"FaceCount":32000,"MaterialType":"不锈钢","FileFormat":"FBX","Length":80,"Width":40,"Height":25}""",
                    CreatedAt = now, CreatedBy = "System" },
                new() { OntologyId = threeD.Id, Name = "设备基座模型", Description = "重型设备混凝土基座模型",
                    PropertiesJson = """{"VertexCount":8500,"FaceCount":6200,"MaterialType":"混凝土","FileFormat":"STL","Length":8,"Width":6,"Height":3}""",
                    CreatedAt = now, CreatedBy = "System" },

                new() { OntologyId = twoD.Id, Name = "一层平面布置图", Description = "厂房一层设备与通道布置",
                    PropertiesJson = """{"DrawingScale":"1:100","LayerCount":12,"AnnotationInfo":"轴线A-J/1-15，标高±0.000","DrawingSize":"A0","FileFormat":"DWG"}""",
                    CreatedAt = now, CreatedBy = "System" },
                new() { OntologyId = twoD.Id, Name = "电气原理图", Description = "主配电系统电气原理",
                    PropertiesJson = """{"DrawingScale":"1:1","LayerCount":8,"AnnotationInfo":"380V/220V配电系统","DrawingSize":"A2","FileFormat":"DWG"}""",
                    CreatedAt = now, CreatedBy = "System" },
                new() { OntologyId = twoD.Id, Name = "管道施工图", Description = "给排水管道施工详图",
                    PropertiesJson = """{"DrawingScale":"1:50","LayerCount":10,"AnnotationInfo":"DN50-DN200管线","DrawingSize":"A1","FileFormat":"PDF"}""",
                    CreatedAt = now, CreatedBy = "System" },

                new() { OntologyId = sim.Id, Name = "流场分析模型", Description = "管道内部流体仿真",
                    PropertiesJson = """{"SimulationType":"流体","ParamConfig":"{\"velocity\":2.5,\"density\":1000,\"viscosity\":0.001}","InputCount":5,"OutputCount":3,"SolverType":"Fluent"}""",
                    CreatedAt = now, CreatedBy = "System" },
                new() { OntologyId = sim.Id, Name = "结构应力分析", Description = "钢结构承重仿真",
                    PropertiesJson = """{"SimulationType":"结构","ParamConfig":"{\"load\":5000,\"material\":\"Q345\",\"safetyFactor\":1.5}","InputCount":6,"OutputCount":4,"SolverType":"ANSYS"}""",
                    CreatedAt = now, CreatedBy = "System" },
                new() { OntologyId = sim.Id, Name = "热传导仿真", Description = "换热器热力学分析",
                    PropertiesJson = """{"SimulationType":"热力学","ParamConfig":"{\"inletTemp\":350,\"outletTemp\":280,\"flowRate\":0.5}","InputCount":4,"OutputCount":2,"SolverType":"COMSOL"}""",
                    CreatedAt = now, CreatedBy = "System" },

                new() { OntologyId = arch.Id, Name = "智慧园区体系", Description = "智慧园区整体架构",
                    PropertiesJson = """{"ArchLevel":"顶层体系","RelatedSysCount":8,"InterfaceDef":"{\"protocol\":\"HTTP/gRPC\",\"format\":\"JSON\"}","Version":"2.0","ArchStyle":"微服务"}""",
                    CreatedAt = now, CreatedBy = "System" },
                new() { OntologyId = arch.Id, Name = "数据中台体系", Description = "企业数据中台架构",
                    PropertiesJson = """{"ArchLevel":"平台层","RelatedSysCount":6,"InterfaceDef":"{\"protocol\":\"API Gateway\",\"format\":\"JSON\"}","Version":"1.5","ArchStyle":"分层架构"}""",
                    CreatedAt = now, CreatedBy = "System" },
                new() { OntologyId = arch.Id, Name = "安全保障体系", Description = "信息安全防护架构",
                    PropertiesJson = """{"ArchLevel":"横向体系","RelatedSysCount":4,"InterfaceDef":"{\"protocol\":\"Syslog/SNMP\",\"format\":\"syslog\"}","Version":"1.0","ArchStyle":"事件驱动"}""",
                    CreatedAt = now, CreatedBy = "System" },

                new() { OntologyId = sys.Id, Name = "用户管理系统", Description = "统一用户认证与权限管理",
                    PropertiesJson = """{"SubSysCount":3,"Dependencies":"{\"LDAP\":\"认证源\",\"Redis\":\"缓存\"}","BoundaryCond":"支持10000并发","Version":"3.1","BelongToArch":"基础平台","IntegrationMode":"REST API"}""",
                    CreatedAt = now, CreatedBy = "System" },
                new() { OntologyId = sys.Id, Name = "文件存储系统", Description = "分布式文件存储服务",
                    PropertiesJson = """{"SubSysCount":2,"Dependencies":"{\"MinIO\":\"对象存储\",\"PostgreSQL\":\"元数据\"}","BoundaryCond":"单文件≤5GB","Version":"2.0","BelongToArch":"基础平台","IntegrationMode":"SDK"}""",
                    CreatedAt = now, CreatedBy = "System" },
                new() { OntologyId = sys.Id, Name = "消息推送系统", Description = "实时消息通知服务",
                    PropertiesJson = """{"SubSysCount":2,"Dependencies":"{\"RabbitMQ\":\"消息队列\",\"WebSocket\":\"实时通道\"}","BoundaryCond":"延迟≤500ms","Version":"1.2","BelongToArch":"业务平台","IntegrationMode":"MQ+WebSocket"}""",
                    CreatedAt = now, CreatedBy = "System" },
            };

            dbContext.Set<ObjectInstance>().AddRange(instances);
            await dbContext.SaveChangesAsync(cancellationToken);

            Console.WriteLine($"[SeedData] 对象实例种子数据创建完成，共 {instances.Count} 条");
            Log.Information("[SeedData] 对象实例种子数据创建完成，共 {Count} 条", instances.Count);
        }

        #endregion

        #region 基础枚举

        private static async Task SeedBasicEnumsAsync(MainDbContext dbContext, CancellationToken cancellationToken)
        {
            var existing = await dbContext.Set<BasicEnum>()
                .FirstOrDefaultAsync(e => e.IsSystem, cancellationToken);
            if (existing != null)
            {
                Console.WriteLine("[SeedData] BasicEnum 种子数据已存在，跳过");
                return;
            }

            var now = new DateTime(2026, 5, 26, 0, 0, 0);

            // ---- Relation 枚举 ----
            var relEnum = new BasicEnum
            {
                Name = "关系类型", DisplayName = "关系类型", Code = "Relation",
                Description = "定义本体之间的关系类型，如关联、依赖、包含等",
                IsSystem = true, SortOrder = 1,
                CreatedAt = now, UpdatedAt = now
            };
            dbContext.Set<BasicEnum>().Add(relEnum);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.Set<BasicEnumItem>().AddRange(
                new BasicEnumItem { EnumId = relEnum.Id, Name = "关联",    DisplayName = "关联",    Code = "Association",  SortOrder = 1, CreatedAt = now, UpdatedAt = now },
                new BasicEnumItem { EnumId = relEnum.Id, Name = "依赖",    DisplayName = "依赖",    Code = "Dependency",   SortOrder = 2, CreatedAt = now, UpdatedAt = now },
                new BasicEnumItem { EnumId = relEnum.Id, Name = "包含",    DisplayName = "包含",    Code = "Contains",     SortOrder = 3, CreatedAt = now, UpdatedAt = now },
                new BasicEnumItem { EnumId = relEnum.Id, Name = "实现",    DisplayName = "实现",    Code = "Implements",   SortOrder = 4, CreatedAt = now, UpdatedAt = now },
                new BasicEnumItem { EnumId = relEnum.Id, Name = "衍生",    DisplayName = "衍生",    Code = "Derives",      SortOrder = 5, CreatedAt = now, UpdatedAt = now },
                new BasicEnumItem { EnumId = relEnum.Id, Name = "继承",    DisplayName = "继承",    Code = "Inherits",     SortOrder = 6, CreatedAt = now, UpdatedAt = now },
                new BasicEnumItem { EnumId = relEnum.Id, Name = "引用",    DisplayName = "引用",    Code = "References",   SortOrder = 7, CreatedAt = now, UpdatedAt = now },
                new BasicEnumItem { EnumId = relEnum.Id, Name = "聚合",    DisplayName = "聚合",    Code = "Aggregation",  SortOrder = 8, CreatedAt = now, UpdatedAt = now },
                new BasicEnumItem { EnumId = relEnum.Id, Name = "组合",    DisplayName = "组合",    Code = "Composition",  SortOrder = 9, CreatedAt = now, UpdatedAt = now },
                new BasicEnumItem { EnumId = relEnum.Id, Name = "扩展",    DisplayName = "扩展",    Code = "Extends",      SortOrder = 10, CreatedAt = now, UpdatedAt = now }
            );
            await dbContext.SaveChangesAsync(cancellationToken);

            Console.WriteLine("[SeedData] 基础枚举种子数据创建完成：Relation（10项）");
            Log.Information("[SeedData] 基础枚举种子数据创建完成");
        }

        #endregion
    }
}
