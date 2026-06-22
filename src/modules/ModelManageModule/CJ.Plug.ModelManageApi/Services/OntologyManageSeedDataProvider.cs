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

            // SysML V2 体系设计场景种子数据
            await EnsureSysMLRelationTypesAsync(dbContext, cancellationToken);
            await SeedSysMLV2DataAsync(dbContext, cancellationToken);
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
                new Property { OntologyId = textFile.Id, Name = "编码格式", Code = "Encoding",         PropertyType = "Select",   SortOrder = 1, Value = "UTF-8",  IsRequired = true,  SelectOptions = "[\"UTF-8\",\"GBK\",\"ASCII\"]" },
                new Property { OntologyId = textFile.Id, Name = "文件大小(KB)", Code = "FileSizeKB",   PropertyType = "Number",   SortOrder = 2, Value = "0",      IsRequired = false },
                new Property { OntologyId = textFile.Id, Name = "行数",      Code = "LineCount",        PropertyType = "Number",   SortOrder = 3, Value = "0",      IsRequired = false },
                new Property { OntologyId = textFile.Id, Name = "内容摘要",  Code = "ContentSummary",   PropertyType = "Text",     SortOrder = 4, Value = "",       IsRequired = false }
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
                new Property { OntologyId = threeD.Id, Name = "顶点数",   Code = "VertexCount",   PropertyType = "Number", SortOrder = 1, Value = "0",   IsRequired = false },
                new Property { OntologyId = threeD.Id, Name = "面数",     Code = "FaceCount",     PropertyType = "Number", SortOrder = 2, Value = "0",   IsRequired = false },
                new Property { OntologyId = threeD.Id, Name = "材质类型", Code = "MaterialType",  PropertyType = "Text",   SortOrder = 3, Value = "",    IsRequired = false },
                new Property { OntologyId = threeD.Id, Name = "文件格式", Code = "FileFormat",    PropertyType = "Select", SortOrder = 4, Value = "OBJ", IsRequired = true,  SelectOptions = "[\"OBJ\",\"FBX\",\"GLB\",\"STL\"]" },
                new Property { OntologyId = threeD.Id, Name = "长度",     Code = "Length",        PropertyType = "Number", SortOrder = 5, Value = "0",   IsRequired = false },
                new Property { OntologyId = threeD.Id, Name = "宽度",     Code = "Width",         PropertyType = "Number", SortOrder = 6, Value = "0",   IsRequired = false },
                new Property { OntologyId = threeD.Id, Name = "高度",     Code = "Height",        PropertyType = "Number", SortOrder = 7, Value = "0",   IsRequired = false }
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
                new Property { OntologyId = twoD.Id, Name = "图纸比例", Code = "DrawingScale",  PropertyType = "Text",   SortOrder = 1, Value = "1:1",  IsRequired = false },
                new Property { OntologyId = twoD.Id, Name = "图层数",   Code = "LayerCount",    PropertyType = "Number", SortOrder = 2, Value = "0",    IsRequired = false },
                new Property { OntologyId = twoD.Id, Name = "标注信息", Code = "AnnotationInfo",PropertyType = "Text",   SortOrder = 3, Value = "",     IsRequired = false },
                new Property { OntologyId = twoD.Id, Name = "图纸尺寸", Code = "DrawingSize",   PropertyType = "Select", SortOrder = 4, Value = "A4",   IsRequired = false, SelectOptions = "[\"A0\",\"A1\",\"A2\",\"A3\",\"A4\"]" },
                new Property { OntologyId = twoD.Id, Name = "文件格式", Code = "FileFormat",    PropertyType = "Select", SortOrder = 5, Value = "DWG",  IsRequired = true,  SelectOptions = "[\"DWG\",\"DXF\",\"PDF\"]" }
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
                new Property { OntologyId = sim.Id, Name = "仿真类型",   Code = "SimulationType", PropertyType = "Select",   SortOrder = 1, Value = "流体", IsRequired = true,  SelectOptions = "[\"流体\",\"结构\",\"热力学\",\"电磁\"]" },
                new Property { OntologyId = sim.Id, Name = "参数配置",   Code = "ParamConfig",    PropertyType = "JsonText", SortOrder = 2, Value = "{}",  IsRequired = false },
                new Property { OntologyId = sim.Id, Name = "输入变量数", Code = "InputCount",     PropertyType = "Number",   SortOrder = 3, Value = "0",   IsRequired = false },
                new Property { OntologyId = sim.Id, Name = "输出变量数", Code = "OutputCount",    PropertyType = "Number",   SortOrder = 4, Value = "0",   IsRequired = false },
                new Property { OntologyId = sim.Id, Name = "求解器类型", Code = "SolverType",     PropertyType = "Text",     SortOrder = 5, Value = "",    IsRequired = false }
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
                new Property { OntologyId = arch.Id, Name = "体系层级",   Code = "ArchLevel",      PropertyType = "Text",     SortOrder = 1, Value = "",   IsRequired = false },
                new Property { OntologyId = arch.Id, Name = "关联系统数", Code = "RelatedSysCount", PropertyType = "Number",   SortOrder = 2, Value = "0",  IsRequired = false },
                new Property { OntologyId = arch.Id, Name = "接口定义",   Code = "InterfaceDef",   PropertyType = "JsonText", SortOrder = 3, Value = "{}", IsRequired = false },
                new Property { OntologyId = arch.Id, Name = "版本号",     Code = "Version",        PropertyType = "Text",     SortOrder = 4, Value = "1.0",IsRequired = false },
                new Property { OntologyId = arch.Id, Name = "架构风格",   Code = "ArchStyle",      PropertyType = "Text",     SortOrder = 5, Value = "",   IsRequired = false }
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
                new Property { OntologyId = sys.Id, Name = "子系统数", Code = "SubSysCount",    PropertyType = "Number",   SortOrder = 1, Value = "0",   IsRequired = false },
                new Property { OntologyId = sys.Id, Name = "依赖关系", Code = "Dependencies",   PropertyType = "JsonText", SortOrder = 2, Value = "{}",  IsRequired = false },
                new Property { OntologyId = sys.Id, Name = "边界条件", Code = "BoundaryCond",   PropertyType = "Text",     SortOrder = 3, Value = "",    IsRequired = false },
                new Property { OntologyId = sys.Id, Name = "版本号",   Code = "Version",        PropertyType = "Text",     SortOrder = 4, Value = "1.0", IsRequired = false },
                new Property { OntologyId = sys.Id, Name = "所属体系", Code = "BelongToArch",   PropertyType = "Text",     SortOrder = 5, Value = "",    IsRequired = false },
                new Property { OntologyId = sys.Id, Name = "集成方式", Code = "IntegrationMode",PropertyType = "Text",     SortOrder = 6, Value = "",    IsRequired = false }
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

        #region SysML V2 体系设计场景

        /// <summary>
        /// 确保 Relation 基础枚举中包含 SysML V2 所需的关系类型
        /// </summary>
        private static async Task EnsureSysMLRelationTypesAsync(MainDbContext dbContext, CancellationToken cancellationToken)
        {
            var relEnum = await dbContext.Set<BasicEnum>()
                .FirstOrDefaultAsync(e => e.Code == "Relation", cancellationToken);
            if (relEnum == null) return;

            var existingCodes = await dbContext.Set<BasicEnumItem>()
                .Where(i => i.EnumId == relEnum.Id)
                .Select(i => i.Code)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow.ToLocalTime();
            var sysMLItems = new List<BasicEnumItem>
            {
                new() { EnumId = relEnum.Id, Name = "满足",   DisplayName = "满足",   Code = "Satisfy",      SortOrder = 11, CreatedAt = now, UpdatedAt = now, Description = "SysML V2: 需求满足关系" },
                new() { EnumId = relEnum.Id, Name = "追溯",   DisplayName = "追溯",   Code = "Trace",        SortOrder = 12, CreatedAt = now, UpdatedAt = now, Description = "SysML V2: 追溯关系" },
                new() { EnumId = relEnum.Id, Name = "细化",   DisplayName = "细化",   Code = "Refine",       SortOrder = 13, CreatedAt = now, UpdatedAt = now, Description = "SysML V2: 需求细化关系" },
                new() { EnumId = relEnum.Id, Name = "验证",   DisplayName = "验证",   Code = "Verify",       SortOrder = 14, CreatedAt = now, UpdatedAt = now, Description = "SysML V2: 验证关系" },
                new() { EnumId = relEnum.Id, Name = "连接",   DisplayName = "连接",   Code = "Connect",      SortOrder = 15, CreatedAt = now, UpdatedAt = now, Description = "SysML V2: 端口连接关系" },
                new() { EnumId = relEnum.Id, Name = "绑定",   DisplayName = "绑定",   Code = "Bind",         SortOrder = 16, CreatedAt = now, UpdatedAt = now, Description = "SysML V2: 绑定连接器" },
                new() { EnumId = relEnum.Id, Name = "项流",   DisplayName = "项流",   Code = "ItemFlow",     SortOrder = 17, CreatedAt = now, UpdatedAt = now, Description = "SysML V2: 项流关系" },
                new() { EnumId = relEnum.Id, Name = "控制流", DisplayName = "控制流", Code = "ControlFlow",  SortOrder = 18, CreatedAt = now, UpdatedAt = now, Description = "SysML V2: 控制流关系" },
                new() { EnumId = relEnum.Id, Name = "对象流", DisplayName = "对象流", Code = "ObjectFlow",   SortOrder = 19, CreatedAt = now, UpdatedAt = now, Description = "SysML V2: 对象流关系" },
                new() { EnumId = relEnum.Id, Name = "转换",   DisplayName = "转换",   Code = "Transition",   SortOrder = 20, CreatedAt = now, UpdatedAt = now, Description = "SysML V2: 状态转换关系" },
                new() { EnumId = relEnum.Id, Name = "分配",   DisplayName = "分配",   Code = "Allocate",     SortOrder = 21, CreatedAt = now, UpdatedAt = now, Description = "SysML V2: 分配关系" },
                new() { EnumId = relEnum.Id, Name = "指派",   DisplayName = "指派",   Code = "Assign",       SortOrder = 22, CreatedAt = now, UpdatedAt = now, Description = "SysML V2: 行为指派关系" },
            };

            var toAdd = sysMLItems.Where(i => !existingCodes.Contains(i.Code)).ToList();
            if (toAdd.Count == 0)
            {
                Console.WriteLine("[SeedData] SysML V2 关系类型已存在，跳过扩展");
                return;
            }

            dbContext.Set<BasicEnumItem>().AddRange(toAdd);
            await dbContext.SaveChangesAsync(cancellationToken);
            Console.WriteLine($"[SeedData] SysML V2 关系类型扩展完成，新增 {toAdd.Count} 项");
            Log.Information("[SeedData] SysML V2 关系类型扩展完成，新增 {Count} 项", toAdd.Count);
        }

        /// <summary>
        /// SysML V2 UAV 无人机系统体系设计种子数据
        /// 覆盖结构/接口/需求/行为/状态/约束/视图等 SysML V2 核心建模元素
        /// </summary>
        private static async Task SeedSysMLV2DataAsync(MainDbContext dbContext, CancellationToken cancellationToken)
        {
            var sentinel = await dbContext.Set<Ontology>()
                .FirstOrDefaultAsync(o => o.Code == "UAV_System" && o.IsSystem, cancellationToken);
            if (sentinel != null)
            {
                Console.WriteLine("[SeedData] SysML V2 种子数据已存在，跳过");
                return;
            }

            var now = DateTime.UtcNow.ToLocalTime();

            // ══════════════════════════════════════════════════════
            // Phase 1: 包定义 (M0 - 基础语义)
            // ══════════════════════════════════════════════════════

            var pkgStructure = new Ontology
            {
                Name = "SysML结构建模", DisplayName = "SysML结构建模", Code = "SYSML_Structure",
                Description = "SysML V2 结构建模包：部件定义、端口、接口、连接",
                IsEnabled = true, SortOrder = 1, IsSystem = true,
                Dimension = MetaModelDimension.M0, Category = "SysML V2",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var pkgBehavior = new Ontology
            {
                Name = "SysML行为建模", DisplayName = "SysML行为建模", Code = "SYSML_Behavior",
                Description = "SysML V2 行为建模包：活动、动作、状态机",
                IsEnabled = true, SortOrder = 2, IsSystem = true,
                Dimension = MetaModelDimension.M0, Category = "SysML V2",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var pkgRequirements = new Ontology
            {
                Name = "SysML需求建模", DisplayName = "SysML需求建模", Code = "SYSML_Requirements",
                Description = "SysML V2 需求建模包：需求定义、利益相关方、需求关系",
                IsEnabled = true, SortOrder = 3, IsSystem = true,
                Dimension = MetaModelDimension.M0, Category = "SysML V2",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var pkgInterfaces = new Ontology
            {
                Name = "SysML接口定义", DisplayName = "SysML接口定义", Code = "SYSML_Interfaces",
                Description = "SysML V2 接口定义包：接口规范、端口类型",
                IsEnabled = true, SortOrder = 4, IsSystem = true,
                Dimension = MetaModelDimension.M0, Category = "SysML V2",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var pkgConstraints = new Ontology
            {
                Name = "SysML约束参数化", DisplayName = "SysML约束参数化", Code = "SYSML_Constraints",
                Description = "SysML V2 约束与参数化包：约束块、参数方程",
                IsEnabled = true, SortOrder = 5, IsSystem = true,
                Dimension = MetaModelDimension.M0, Category = "SysML V2",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var pkgViews = new Ontology
            {
                Name = "SysML视图视点", DisplayName = "SysML视图视点", Code = "SYSML_Views",
                Description = "SysML V2 视图与视点包：分析案例、用例、系统视图",
                IsEnabled = true, SortOrder = 6, IsSystem = true,
                Dimension = MetaModelDimension.M0, Category = "SysML V2",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };

            dbContext.Set<Ontology>().AddRange(pkgStructure, pkgBehavior, pkgRequirements, pkgInterfaces, pkgConstraints, pkgViews);
            await dbContext.SaveChangesAsync(cancellationToken);

            // ══════════════════════════════════════════════════════
            // Phase 2: 结构建模 — Part Definitions (M1)
            // ══════════════════════════════════════════════════════

            var uavSystem = new Ontology
            {
                Name = "无人机系统", DisplayName = "无人机系统", Code = "UAV_System",
                Description = "四旋翼无人机整机系统，包含机身、推进、飞控、导航、通信、载荷、电源等子系统",
                IsEnabled = true, SortOrder = 10, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Structure",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var airframe = new Ontology
            {
                Name = "机身结构", DisplayName = "机身结构", Code = "Airframe",
                Description = "无人机机身框架结构，承载所有子系统并提供气动外形",
                IsEnabled = true, SortOrder = 11, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Structure",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var propulsion = new Ontology
            {
                Name = "推进系统", DisplayName = "推进系统", Code = "PropulsionSystem",
                Description = "无刷电机+ESC+螺旋桨组成的动力推进单元",
                IsEnabled = true, SortOrder = 12, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Structure",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var flightCtrl = new Ontology
            {
                Name = "飞控系统", DisplayName = "飞控系统", Code = "FlightController",
                Description = "飞行控制计算机，执行姿态解算、控制律计算与指令分发",
                IsEnabled = true, SortOrder = 13, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Structure",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var navigation = new Ontology
            {
                Name = "导航模块", DisplayName = "导航模块", Code = "NavigationModule",
                Description = "GPS+IMU+磁力计组合导航定位模块",
                IsEnabled = true, SortOrder = 14, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Structure",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var communication = new Ontology
            {
                Name = "通信模块", DisplayName = "通信模块", Code = "CommunicationModule",
                Description = "数传电台+图传模块，实现空地双向通信",
                IsEnabled = true, SortOrder = 15, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Structure",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var payload = new Ontology
            {
                Name = "载荷系统", DisplayName = "载荷系统", Code = "Payload",
                Description = "任务载荷挂载系统，支持相机、传感器等即插即用",
                IsEnabled = true, SortOrder = 16, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Structure",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var powerSystem = new Ontology
            {
                Name = "电源系统", DisplayName = "电源系统", Code = "PowerSystem",
                Description = "锂电池+电源管理模块，为整机提供稳压供电",
                IsEnabled = true, SortOrder = 17, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Structure",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var landingGear = new Ontology
            {
                Name = "起落架", DisplayName = "起落架", Code = "LandingGear",
                Description = "着陆支撑结构，吸收着陆冲击",
                IsEnabled = true, SortOrder = 18, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Structure",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var groundStation = new Ontology
            {
                Name = "地面站", DisplayName = "地面站", Code = "GroundStation",
                Description = "地面控制站，运行任务规划与监控软件",
                IsEnabled = true, SortOrder = 19, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Structure",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var camera = new Ontology
            {
                Name = "相机载荷", DisplayName = "相机载荷", Code = "Camera",
                Description = "高分辨率航拍相机，支持4K视频与RAW照片",
                IsEnabled = true, SortOrder = 20, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Structure",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var gpsReceiver = new Ontology
            {
                Name = "GPS接收机", DisplayName = "GPS接收机", Code = "GPSReceiver",
                Description = "多星座GNSS接收机模块，支持GPS/GLONASS/北斗",
                IsEnabled = true, SortOrder = 21, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Structure",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };

            dbContext.Set<Ontology>().AddRange(
                uavSystem, airframe, propulsion, flightCtrl, navigation, communication,
                payload, powerSystem, landingGear, groundStation, camera, gpsReceiver);
            await dbContext.SaveChangesAsync(cancellationToken);

            // ══════════════════════════════════════════════════════
            // Phase 3: 端口定义 (M1)
            // ══════════════════════════════════════════════════════

            var gpsPort = new Ontology
            {
                Name = "GPS数据端口", DisplayName = "GPS数据端口", Code = "GPS_DataPort",
                Description = "GPS接收机数据输出端口",
                IsEnabled = true, SortOrder = 30, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Port",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var imuPort = new Ontology
            {
                Name = "IMU数据端口", DisplayName = "IMU数据端口", Code = "IMU_DataPort",
                Description = "惯性测量单元数据输出端口",
                IsEnabled = true, SortOrder = 31, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Port",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var motorCtrlPort = new Ontology
            {
                Name = "电机控制端口", DisplayName = "电机控制端口", Code = "MotorControl_Port",
                Description = "飞控输出PWM信号至电调的端口",
                IsEnabled = true, SortOrder = 32, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Port",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var telemetryPort = new Ontology
            {
                Name = "遥测端口", DisplayName = "遥测端口", Code = "Telemetry_Port",
                Description = "飞行数据遥测下发端口",
                IsEnabled = true, SortOrder = 33, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Port",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var commandPort = new Ontology
            {
                Name = "指令端口", DisplayName = "指令端口", Code = "Command_Port",
                Description = "地面站上行指令接收端口",
                IsEnabled = true, SortOrder = 34, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Port",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var videoPort = new Ontology
            {
                Name = "视频流端口", DisplayName = "视频流端口", Code = "VideoStream_Port",
                Description = "相机视频流输出端口",
                IsEnabled = true, SortOrder = 35, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Port",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var powerSupplyPort = new Ontology
            {
                Name = "供电端口", DisplayName = "供电端口", Code = "PowerSupply_Port",
                Description = "电源系统供电输出端口",
                IsEnabled = true, SortOrder = 36, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Port",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var powerConsumePort = new Ontology
            {
                Name = "用电端口", DisplayName = "用电端口", Code = "PowerConsumer_Port",
                Description = "子系统用电输入端口",
                IsEnabled = true, SortOrder = 37, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Port",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };

            dbContext.Set<Ontology>().AddRange(
                gpsPort, imuPort, motorCtrlPort, telemetryPort, commandPort,
                videoPort, powerSupplyPort, powerConsumePort);
            await dbContext.SaveChangesAsync(cancellationToken);

            // ══════════════════════════════════════════════════════
            // Phase 4: 接口定义 (M1)
            // ══════════════════════════════════════════════════════

            var ifNav = new Ontology
            {
                Name = "导航数据接口", DisplayName = "导航数据接口", Code = "INavigationData",
                Description = "导航数据接口规范：位置、速度、姿态四元数",
                IsEnabled = true, SortOrder = 40, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Interface",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var ifThrust = new Ontology
            {
                Name = "推力控制接口", DisplayName = "推力控制接口", Code = "IThrustControl",
                Description = "电机推力控制接口：PWM占空比、转速反馈",
                IsEnabled = true, SortOrder = 41, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Interface",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var ifTelemetry = new Ontology
            {
                Name = "遥测接口", DisplayName = "遥测接口", Code = "ITelemetry",
                Description = "遥测数据传输接口：飞行参数、系统状态、心跳包",
                IsEnabled = true, SortOrder = 42, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Interface",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var ifPower = new Ontology
            {
                Name = "电力分配接口", DisplayName = "电力分配接口", Code = "IPowerDistribution",
                Description = "电源分配接口：电压、电流、剩余电量",
                IsEnabled = true, SortOrder = 43, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Interface",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var ifVideo = new Ontology
            {
                Name = "视频流接口", DisplayName = "视频流接口", Code = "IVideoStream",
                Description = "视频流传输接口：H.264/H.265码流、分辨率、帧率",
                IsEnabled = true, SortOrder = 44, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Interface",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var ifCommand = new Ontology
            {
                Name = "指令接口", DisplayName = "指令接口", Code = "ICommand",
                Description = "地面站指令接口：飞行模式、航点、紧急指令",
                IsEnabled = true, SortOrder = 45, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Interface",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };

            dbContext.Set<Ontology>().AddRange(ifNav, ifThrust, ifTelemetry, ifPower, ifVideo, ifCommand);
            await dbContext.SaveChangesAsync(cancellationToken);

            // ══════════════════════════════════════════════════════
            // Phase 5: 需求定义 (M1)
            // ══════════════════════════════════════════════════════

            var reqMaxSpeed = new Ontology
            {
                Name = "REQ-最大飞行速度", DisplayName = "REQ-最大飞行速度", Code = "REQ-MaxSpeed",
                Description = "无人机最大水平飞行速度须 ≥ 80 km/h",
                IsEnabled = true, SortOrder = 50, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Requirement",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var reqMaxAlt = new Ontology
            {
                Name = "REQ-最大升限", DisplayName = "REQ-最大升限", Code = "REQ-MaxAltitude",
                Description = "无人机最大飞行海拔高度须 ≥ 500 m",
                IsEnabled = true, SortOrder = 51, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Requirement",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var reqEndurance = new Ontology
            {
                Name = "REQ-续航时间", DisplayName = "REQ-续航时间", Code = "REQ-Endurance",
                Description = "标准载荷下续航时间须 ≥ 60 分钟",
                IsEnabled = true, SortOrder = 52, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Requirement",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var reqCommRange = new Ontology
            {
                Name = "REQ-通信距离", DisplayName = "REQ-通信距离", Code = "REQ-CommunicationRange",
                Description = "空地通信距离（无遮挡）须 ≥ 10 km",
                IsEnabled = true, SortOrder = 53, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Requirement",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var reqPayload = new Ontology
            {
                Name = "REQ-载荷能力", DisplayName = "REQ-载荷能力", Code = "REQ-PayloadCapacity",
                Description = "有效载荷重量须 ≥ 5 kg",
                IsEnabled = true, SortOrder = 54, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Requirement",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var reqSafety = new Ontology
            {
                Name = "REQ-安全降落", DisplayName = "REQ-安全降落", Code = "REQ-SafetyLanding",
                Description = "系统须具备低电量自动返航与安全降落能力",
                IsEnabled = true, SortOrder = 55, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Requirement",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var reqGPS = new Ontology
            {
                Name = "REQ-定位精度", DisplayName = "REQ-定位精度", Code = "REQ-GPSAccuracy",
                Description = "GPS水平定位精度须 ≤ 2 m（RTK模式）",
                IsEnabled = true, SortOrder = 56, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Requirement",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var reqEmergency = new Ontology
            {
                Name = "REQ-紧急返航", DisplayName = "REQ-紧急返航", Code = "REQ-EmergencyReturn",
                Description = "通信中断超过 5 秒须自动触发紧急返航程序",
                IsEnabled = true, SortOrder = 57, IsSystem = true,
                Dimension = MetaModelDimension.M1, Category = "SysML V2 / Requirement",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };

            dbContext.Set<Ontology>().AddRange(
                reqMaxSpeed, reqMaxAlt, reqEndurance, reqCommRange,
                reqPayload, reqSafety, reqGPS, reqEmergency);
            await dbContext.SaveChangesAsync(cancellationToken);

            // ══════════════════════════════════════════════════════
            // Phase 6: 状态定义 (M2)
            // ══════════════════════════════════════════════════════

            var stGrounded = new Ontology
            {
                Name = "地面待命", DisplayName = "地面待命", Code = "ST-Grounded",
                Description = "无人机上电自检完成，等待起飞指令（初始状态）",
                IsEnabled = true, SortOrder = 60, IsSystem = true,
                Dimension = MetaModelDimension.M2, Category = "SysML V2 / State",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var stTakingOff = new Ontology
            {
                Name = "起飞中", DisplayName = "起飞中", Code = "ST-TakingOff",
                Description = "电机加速至起飞推力，离地爬升",
                IsEnabled = true, SortOrder = 61, IsSystem = true,
                Dimension = MetaModelDimension.M2, Category = "SysML V2 / State",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var stCruising = new Ontology
            {
                Name = "巡航中", DisplayName = "巡航中", Code = "ST-Cruising",
                Description = "按航点/航线稳定飞行",
                IsEnabled = true, SortOrder = 62, IsSystem = true,
                Dimension = MetaModelDimension.M2, Category = "SysML V2 / State",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var stLanding = new Ontology
            {
                Name = "降落中", DisplayName = "降落中", Code = "ST-Landing",
                Description = "减速下降至着陆触地",
                IsEnabled = true, SortOrder = 63, IsSystem = true,
                Dimension = MetaModelDimension.M2, Category = "SysML V2 / State",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var stEmergency = new Ontology
            {
                Name = "紧急状态", DisplayName = "紧急状态", Code = "ST-Emergency",
                Description = "检测到严重异常（通信中断、电量极低），执行紧急预案",
                IsEnabled = true, SortOrder = 64, IsSystem = true,
                Dimension = MetaModelDimension.M2, Category = "SysML V2 / State",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };

            dbContext.Set<Ontology>().AddRange(stGrounded, stTakingOff, stCruising, stLanding, stEmergency);
            await dbContext.SaveChangesAsync(cancellationToken);

            // ══════════════════════════════════════════════════════
            // Phase 7: 用例定义 (M4)
            // ══════════════════════════════════════════════════════

            var ucTakeOff = new Ontology
            {
                Name = "起飞", DisplayName = "起飞", Code = "UC-TakeOff",
                Description = "操作员通过地面站发送起飞指令，无人机执行起飞序列",
                IsEnabled = true, SortOrder = 70, IsSystem = true,
                Dimension = MetaModelDimension.M4, Category = "SysML V2 / UseCase",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var ucLand = new Ontology
            {
                Name = "降落", DisplayName = "降落", Code = "UC-Land",
                Description = "操作员发送降落指令或自动触发降落",
                IsEnabled = true, SortOrder = 71, IsSystem = true,
                Dimension = MetaModelDimension.M4, Category = "SysML V2 / UseCase",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var ucCruise = new Ontology
            {
                Name = "巡航", DisplayName = "巡航", Code = "UC-Cruise",
                Description = "无人机按预设航线自动巡航飞行",
                IsEnabled = true, SortOrder = 72, IsSystem = true,
                Dimension = MetaModelDimension.M4, Category = "SysML V2 / UseCase",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var ucReturnHome = new Ontology
            {
                Name = "返航", DisplayName = "返航", Code = "UC-ReturnHome",
                Description = "触发自动返航，返回起飞点并降落",
                IsEnabled = true, SortOrder = 73, IsSystem = true,
                Dimension = MetaModelDimension.M4, Category = "SysML V2 / UseCase",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var ucSurvey = new Ontology
            {
                Name = "测绘任务", DisplayName = "测绘任务", Code = "UC-SurveyMission",
                Description = "执行航测任务：航线规划→自动拍照→数据回传",
                IsEnabled = true, SortOrder = 74, IsSystem = true,
                Dimension = MetaModelDimension.M4, Category = "SysML V2 / UseCase",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };

            dbContext.Set<Ontology>().AddRange(ucTakeOff, ucLand, ucCruise, ucReturnHome, ucSurvey);
            await dbContext.SaveChangesAsync(cancellationToken);

            // ══════════════════════════════════════════════════════
            // Phase 8: 分析案例 (M4)
            // ══════════════════════════════════════════════════════

            var acWeight = new Ontology
            {
                Name = "重量平衡分析", DisplayName = "重量平衡分析", Code = "AC-WeightBalance",
                Description = "分析整机重量分布，验证重心在推力中心±5mm范围内",
                IsEnabled = true, SortOrder = 80, IsSystem = true,
                Dimension = MetaModelDimension.M4, Category = "SysML V2 / AnalysisCase",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var acThrust = new Ontology
            {
                Name = "推力计算分析", DisplayName = "推力计算分析", Code = "AC-ThrustCalculation",
                Description = "计算各工况下所需推力，验证动力系统是否满足起飞、悬停、机动需求",
                IsEnabled = true, SortOrder = 81, IsSystem = true,
                Dimension = MetaModelDimension.M4, Category = "SysML V2 / AnalysisCase",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var acBattery = new Ontology
            {
                Name = "电池续航模型", DisplayName = "电池续航模型", Code = "AC-BatteryLifeModel",
                Description = "建立电池放电模型，验证续航时间是否满足60分钟需求",
                IsEnabled = true, SortOrder = 82, IsSystem = true,
                Dimension = MetaModelDimension.M4, Category = "SysML V2 / AnalysisCase",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };

            dbContext.Set<Ontology>().AddRange(acWeight, acThrust, acBattery);
            await dbContext.SaveChangesAsync(cancellationToken);

            // ══════════════════════════════════════════════════════
            // Phase 9: 约束块 (M3)
            // ══════════════════════════════════════════════════════

            var cbWeight = new Ontology
            {
                Name = "总重约束", DisplayName = "总重约束", Code = "CB-WeightLimit",
                Description = "约束：总起飞重量 m_total ≤ 单电机最大推力 × 电机数 × 0.7",
                IsEnabled = true, SortOrder = 90, IsSystem = true,
                Dimension = MetaModelDimension.M3, Category = "SysML V2 / ConstraintBlock",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var cbPower = new Ontology
            {
                Name = "功耗预算约束", DisplayName = "功耗预算约束", Code = "CB-PowerBudget",
                Description = "约束：Σ(各子系统功耗) ≤ 电池持续输出功率 × 0.85",
                IsEnabled = true, SortOrder = 91, IsSystem = true,
                Dimension = MetaModelDimension.M3, Category = "SysML V2 / ConstraintBlock",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };
            var cbCG = new Ontology
            {
                Name = "重心约束", DisplayName = "重心约束", Code = "CB-CGConstraint",
                Description = "约束：|CG_x - TC_x| ≤ 5mm, |CG_y - TC_y| ≤ 5mm（CG偏离推力中心≤5mm）",
                IsEnabled = true, SortOrder = 92, IsSystem = true,
                Dimension = MetaModelDimension.M3, Category = "SysML V2 / ConstraintBlock",
                CreatedAt = now, UpdatedAt = now, CreatedBy = "System"
            };

            dbContext.Set<Ontology>().AddRange(cbWeight, cbPower, cbCG);
            await dbContext.SaveChangesAsync(cancellationToken);

            // ══════════════════════════════════════════════════════
            // Phase 10: 属性字段
            // ══════════════════════════════════════════════════════

            var allProperties = new List<Property>();

            // --- Structure PartDef 属性 ---
            allProperties.AddRange(new[]
            {
                new Property { OntologyId = uavSystem.Id,   Name = "起飞重量(kg)", Code = "TakeoffWeight",    PropertyType = "Number",   SortOrder = 1, Value = "0",  IsRequired = false },
                new Property { OntologyId = uavSystem.Id,   Name = "轴距(mm)",     Code = "Wheelbase",        PropertyType = "Number",   SortOrder = 2, Value = "0",  IsRequired = false },
                new Property { OntologyId = uavSystem.Id,   Name = "制造商",       Code = "Manufacturer",     PropertyType = "Text",     SortOrder = 3, Value = "",   IsRequired = false },
                new Property { OntologyId = uavSystem.Id,   Name = "型号",         Code = "Model",            PropertyType = "Text",     SortOrder = 4, Value = "",   IsRequired = false },
                new Property { OntologyId = airframe.Id,    Name = "材质",         Code = "Material",         PropertyType = "Select",   SortOrder = 1, Value = "碳纤维", IsRequired = true, SelectOptions = """["碳纤维","铝合金","工程塑料","钛合金"]""" },
                new Property { OntologyId = airframe.Id,    Name = "重量(g)",      Code = "WeightGrams",      PropertyType = "Number",   SortOrder = 2, Value = "0",  IsRequired = false },
                new Property { OntologyId = propulsion.Id,  Name = "电机型号",     Code = "MotorType",        PropertyType = "Text",     SortOrder = 1, Value = "",   IsRequired = false },
                new Property { OntologyId = propulsion.Id,  Name = "最大推力(N)",  Code = "MaxThrustN",       PropertyType = "Number",   SortOrder = 2, Value = "0",  IsRequired = false },
                new Property { OntologyId = propulsion.Id,  Name = "电调型号",     Code = "ESCType",          PropertyType = "Text",     SortOrder = 3, Value = "",   IsRequired = false },
                new Property { OntologyId = propulsion.Id,  Name = "螺旋桨尺寸",   Code = "PropellerSize",    PropertyType = "Text",     SortOrder = 4, Value = "",   IsRequired = false },
                new Property { OntologyId = flightCtrl.Id,  Name = "MCU型号",      Code = "MCUType",          PropertyType = "Text",     SortOrder = 1, Value = "",   IsRequired = false },
                new Property { OntologyId = flightCtrl.Id,  Name = "固件版本",     Code = "FirmwareVer",      PropertyType = "Text",     SortOrder = 2, Value = "1.0",IsRequired = false },
                new Property { OntologyId = flightCtrl.Id,  Name = "采样频率(Hz)", Code = "SampleRateHz",     PropertyType = "Number",   SortOrder = 3, Value = "400",IsRequired = false },
                new Property { OntologyId = navigation.Id,  Name = "传感器列表",   Code = "SensorList",       PropertyType = "JsonText", SortOrder = 1, Value = "{}", IsRequired = false },
                new Property { OntologyId = navigation.Id,  Name = "更新频率(Hz)", Code = "UpdateRateHz",     PropertyType = "Number",   SortOrder = 2, Value = "100",IsRequired = false },
                new Property { OntologyId = communication.Id, Name = "频段",       Code = "FrequencyBand",    PropertyType = "Select",   SortOrder = 1, Value = "2.4GHz", IsRequired = true, SelectOptions = """["2.4GHz","5.8GHz","433MHz","900MHz"]""" },
                new Property { OntologyId = communication.Id, Name = "发射功率(dBm)", Code = "TxPowerdBm",    PropertyType = "Number",   SortOrder = 2, Value = "20", IsRequired = false },
                new Property { OntologyId = communication.Id, Name = "协议",       Code = "Protocol",         PropertyType = "Text",     SortOrder = 3, Value = "MAVLink", IsRequired = false },
                new Property { OntologyId = payload.Id,     Name = "最大载荷(g)",  Code = "MaxPayloadGrams",  PropertyType = "Number",   SortOrder = 1, Value = "0",  IsRequired = false },
                new Property { OntologyId = payload.Id,     Name = "挂载接口类型", Code = "MountType",        PropertyType = "Select",   SortOrder = 2, Value = "快拆", IsRequired = true, SelectOptions = """["快拆","螺丝固定","磁吸","导轨"]""" },
                new Property { OntologyId = powerSystem.Id, Name = "电池类型",     Code = "BatteryType",      PropertyType = "Select",   SortOrder = 1, Value = "LiPo", IsRequired = true, SelectOptions = """["LiPo","LiIon","LiHV","NiMH"]""" },
                new Property { OntologyId = powerSystem.Id, Name = "容量(mAh)",    Code = "CapacitymAh",      PropertyType = "Number",   SortOrder = 2, Value = "0",  IsRequired = false },
                new Property { OntologyId = powerSystem.Id, Name = "标称电压(V)",  Code = "NominalVoltage",   PropertyType = "Number",   SortOrder = 3, Value = "0",  IsRequired = false },
                new Property { OntologyId = landingGear.Id, Name = "类型",         Code = "GearType",         PropertyType = "Select",   SortOrder = 1, Value = "滑橇式", IsRequired = true, SelectOptions = """["滑橇式","轮式","折叠式","固定式"]""" },
                new Property { OntologyId = landingGear.Id, Name = "缓冲行程(mm)", Code = "BufferTravelMm",   PropertyType = "Number",   SortOrder = 2, Value = "0",  IsRequired = false },
                new Property { OntologyId = groundStation.Id, Name = "软件名称",   Code = "SoftwareName",     PropertyType = "Text",     SortOrder = 1, Value = "",   IsRequired = false },
                new Property { OntologyId = groundStation.Id, Name = "操作系统",   Code = "OSType",           PropertyType = "Select",   SortOrder = 2, Value = "Windows", IsRequired = true, SelectOptions = """["Windows","Linux","macOS","Android","iOS"]""" },
                new Property { OntologyId = camera.Id,      Name = "分辨率",       Code = "Resolution",       PropertyType = "Select",   SortOrder = 1, Value = "4K", IsRequired = true, SelectOptions = """["4K","1080p","720p","2.7K"]""" },
                new Property { OntologyId = camera.Id,      Name = "传感器尺寸",   Code = "SensorSize",       PropertyType = "Text",     SortOrder = 2, Value = "",   IsRequired = false },
                new Property { OntologyId = gpsReceiver.Id, Name = "支持星座",     Code = "Constellations",   PropertyType = "Text",     SortOrder = 1, Value = "GPS+GLONASS+北斗", IsRequired = false },
                new Property { OntologyId = gpsReceiver.Id, Name = "更新频率(Hz)", Code = "UpdateRateHz",     PropertyType = "Number",   SortOrder = 2, Value = "10", IsRequired = false },
            });

            // --- Port 属性 ---
            allProperties.AddRange(new[]
            {
                new Property { OntologyId = gpsPort.Id,        Name = "方向",     Code = "Direction",     PropertyType = "Select",  SortOrder = 1, Value = "Out", IsRequired = true, SelectOptions = """["In","Out","InOut"]""" },
                new Property { OntologyId = gpsPort.Id,        Name = "数据协议", Code = "DataProtocol",  PropertyType = "Text",    SortOrder = 2, Value = "NMEA", IsRequired = false },
                new Property { OntologyId = imuPort.Id,        Name = "方向",     Code = "Direction",     PropertyType = "Select",  SortOrder = 1, Value = "Out", IsRequired = true, SelectOptions = """["In","Out","InOut"]""" },
                new Property { OntologyId = imuPort.Id,        Name = "数据协议", Code = "DataProtocol",  PropertyType = "Text",    SortOrder = 2, Value = "SPI", IsRequired = false },
                new Property { OntologyId = motorCtrlPort.Id,  Name = "方向",     Code = "Direction",     PropertyType = "Select",  SortOrder = 1, Value = "Out", IsRequired = true, SelectOptions = """["In","Out","InOut"]""" },
                new Property { OntologyId = motorCtrlPort.Id,  Name = "信号类型", Code = "SignalType",    PropertyType = "Text",    SortOrder = 2, Value = "PWM", IsRequired = false },
                new Property { OntologyId = telemetryPort.Id,  Name = "方向",     Code = "Direction",     PropertyType = "Select",  SortOrder = 1, Value = "Out", IsRequired = true, SelectOptions = """["In","Out","InOut"]""" },
                new Property { OntologyId = telemetryPort.Id,  Name = "波特率",   Code = "BaudRate",      PropertyType = "Number",  SortOrder = 2, Value = "115200", IsRequired = false },
                new Property { OntologyId = commandPort.Id,    Name = "方向",     Code = "Direction",     PropertyType = "Select",  SortOrder = 1, Value = "In",  IsRequired = true, SelectOptions = """["In","Out","InOut"]""" },
                new Property { OntologyId = commandPort.Id,    Name = "波特率",   Code = "BaudRate",      PropertyType = "Number",  SortOrder = 2, Value = "115200", IsRequired = false },
                new Property { OntologyId = videoPort.Id,      Name = "方向",     Code = "Direction",     PropertyType = "Select",  SortOrder = 1, Value = "Out", IsRequired = true, SelectOptions = """["In","Out","InOut"]""" },
                new Property { OntologyId = videoPort.Id,      Name = "编码格式", Code = "CodecFormat",   PropertyType = "Select",  SortOrder = 2, Value = "H.264", IsRequired = false, SelectOptions = """["H.264","H.265","MJPEG"]""" },
                new Property { OntologyId = powerSupplyPort.Id, Name = "方向",    Code = "Direction",     PropertyType = "Select",  SortOrder = 1, Value = "Out", IsRequired = true, SelectOptions = """["In","Out","InOut"]""" },
                new Property { OntologyId = powerSupplyPort.Id, Name = "电压(V)", Code = "VoltageV",      PropertyType = "Number",  SortOrder = 2, Value = "0",  IsRequired = false },
                new Property { OntologyId = powerConsumePort.Id, Name = "方向",   Code = "Direction",     PropertyType = "Select",  SortOrder = 1, Value = "In",  IsRequired = true, SelectOptions = """["In","Out","InOut"]""" },
                new Property { OntologyId = powerConsumePort.Id, Name = "功耗(W)", Code = "PowerDrawW",    PropertyType = "Number",  SortOrder = 2, Value = "0",  IsRequired = false },
            });

            // --- Interface 属性 ---
            allProperties.AddRange(new[]
            {
                new Property { OntologyId = ifNav.Id,     Name = "数据字段",   Code = "DataFields",    PropertyType = "JsonText", SortOrder = 1, Value = """{"position":"Vector3","velocity":"Vector3","attitude":"Quaternion"}""", IsRequired = false },
                new Property { OntologyId = ifNav.Id,     Name = "协议版本",   Code = "ProtocolVer",   PropertyType = "Text",     SortOrder = 2, Value = "1.0", IsRequired = false },
                new Property { OntologyId = ifThrust.Id,  Name = "控制参数",   Code = "ControlParams", PropertyType = "JsonText", SortOrder = 1, Value = """{"dutyCycle":"float","rpmFeedback":"int"}""", IsRequired = false },
                new Property { OntologyId = ifTelemetry.Id, Name = "遥测字段", Code = "TelemetryFields",PropertyType = "JsonText", SortOrder = 1, Value = """{"altitude":"float","speed":"float","battery":"int","gpsFix":"int"}""", IsRequired = false },
                new Property { OntologyId = ifPower.Id,   Name = "电量字段",   Code = "PowerFields",   PropertyType = "JsonText", SortOrder = 1, Value = """{"voltage":"float","current":"float","remainingPct":"int"}""", IsRequired = false },
                new Property { OntologyId = ifVideo.Id,   Name = "流参数",     Code = "StreamParams",  PropertyType = "JsonText", SortOrder = 1, Value = """{"codec":"H.264","resolution":"3840x2160","fps":30,"bitrate":"100Mbps"}""", IsRequired = false },
                new Property { OntologyId = ifCommand.Id, Name = "指令集",     Code = "CommandSet",    PropertyType = "JsonText", SortOrder = 1, Value = """["Arm","Disarm","Takeoff","Land","GoTo","ReturnHome"]""", IsRequired = false },
            });

            // --- Requirement 属性 ---
            allProperties.AddRange(new[]
            {
                new Property { OntologyId = reqMaxSpeed.Id,    Name = "优先级",     Code = "Priority",         PropertyType = "Select", SortOrder = 1, Value = "高", IsRequired = true, SelectOptions = """["关键","高","中","低"]""" },
                new Property { OntologyId = reqMaxSpeed.Id,    Name = "验证方法",   Code = "VerificationMethod",PropertyType = "Select", SortOrder = 2, Value = "测试", IsRequired = false, SelectOptions = """["测试","分析","检查","演示"]""" },
                new Property { OntologyId = reqMaxSpeed.Id,    Name = "状态",       Code = "Status",           PropertyType = "Select", SortOrder = 3, Value = "已批准", IsRequired = false, SelectOptions = """["草稿","评审中","已批准","已验证","已废弃"]""" },
                new Property { OntologyId = reqMaxAlt.Id,      Name = "优先级",     Code = "Priority",         PropertyType = "Select", SortOrder = 1, Value = "高", IsRequired = true, SelectOptions = """["关键","高","中","低"]""" },
                new Property { OntologyId = reqMaxAlt.Id,      Name = "验证方法",   Code = "VerificationMethod",PropertyType = "Select", SortOrder = 2, Value = "测试", IsRequired = false, SelectOptions = """["测试","分析","检查","演示"]""" },
                new Property { OntologyId = reqMaxAlt.Id,      Name = "状态",       Code = "Status",           PropertyType = "Select", SortOrder = 3, Value = "已批准", IsRequired = false, SelectOptions = """["草稿","评审中","已批准","已验证","已废弃"]""" },
                new Property { OntologyId = reqEndurance.Id,   Name = "优先级",     Code = "Priority",         PropertyType = "Select", SortOrder = 1, Value = "关键", IsRequired = true, SelectOptions = """["关键","高","中","低"]""" },
                new Property { OntologyId = reqEndurance.Id,   Name = "验证方法",   Code = "VerificationMethod",PropertyType = "Select", SortOrder = 2, Value = "测试", IsRequired = false, SelectOptions = """["测试","分析","检查","演示"]""" },
                new Property { OntologyId = reqEndurance.Id,   Name = "状态",       Code = "Status",           PropertyType = "Select", SortOrder = 3, Value = "已批准", IsRequired = false, SelectOptions = """["草稿","评审中","已批准","已验证","已废弃"]""" },
                new Property { OntologyId = reqCommRange.Id,   Name = "优先级",     Code = "Priority",         PropertyType = "Select", SortOrder = 1, Value = "高", IsRequired = true, SelectOptions = """["关键","高","中","低"]""" },
                new Property { OntologyId = reqCommRange.Id,   Name = "验证方法",   Code = "VerificationMethod",PropertyType = "Select", SortOrder = 2, Value = "测试", IsRequired = false, SelectOptions = """["测试","分析","检查","演示"]""" },
                new Property { OntologyId = reqCommRange.Id,   Name = "状态",       Code = "Status",           PropertyType = "Select", SortOrder = 3, Value = "已批准", IsRequired = false, SelectOptions = """["草稿","评审中","已批准","已验证","已废弃"]""" },
                new Property { OntologyId = reqPayload.Id,     Name = "优先级",     Code = "Priority",         PropertyType = "Select", SortOrder = 1, Value = "关键", IsRequired = true, SelectOptions = """["关键","高","中","低"]""" },
                new Property { OntologyId = reqPayload.Id,     Name = "验证方法",   Code = "VerificationMethod",PropertyType = "Select", SortOrder = 2, Value = "测试", IsRequired = false, SelectOptions = """["测试","分析","检查","演示"]""" },
                new Property { OntologyId = reqPayload.Id,     Name = "状态",       Code = "Status",           PropertyType = "Select", SortOrder = 3, Value = "已批准", IsRequired = false, SelectOptions = """["草稿","评审中","已批准","已验证","已废弃"]""" },
                new Property { OntologyId = reqSafety.Id,      Name = "优先级",     Code = "Priority",         PropertyType = "Select", SortOrder = 1, Value = "关键", IsRequired = true, SelectOptions = """["关键","高","中","低"]""" },
                new Property { OntologyId = reqSafety.Id,      Name = "验证方法",   Code = "VerificationMethod",PropertyType = "Select", SortOrder = 2, Value = "测试", IsRequired = false, SelectOptions = """["测试","分析","检查","演示"]""" },
                new Property { OntologyId = reqSafety.Id,      Name = "状态",       Code = "Status",           PropertyType = "Select", SortOrder = 3, Value = "已批准", IsRequired = false, SelectOptions = """["草稿","评审中","已批准","已验证","已废弃"]""" },
                new Property { OntologyId = reqGPS.Id,         Name = "优先级",     Code = "Priority",         PropertyType = "Select", SortOrder = 1, Value = "中", IsRequired = true, SelectOptions = """["关键","高","中","低"]""" },
                new Property { OntologyId = reqGPS.Id,         Name = "验证方法",   Code = "VerificationMethod",PropertyType = "Select", SortOrder = 2, Value = "测试", IsRequired = false, SelectOptions = """["测试","分析","检查","演示"]""" },
                new Property { OntologyId = reqGPS.Id,         Name = "状态",       Code = "Status",           PropertyType = "Select", SortOrder = 3, Value = "已批准", IsRequired = false, SelectOptions = """["草稿","评审中","已批准","已验证","已废弃"]""" },
                new Property { OntologyId = reqEmergency.Id,   Name = "优先级",     Code = "Priority",         PropertyType = "Select", SortOrder = 1, Value = "关键", IsRequired = true, SelectOptions = """["关键","高","中","低"]""" },
                new Property { OntologyId = reqEmergency.Id,   Name = "验证方法",   Code = "VerificationMethod",PropertyType = "Select", SortOrder = 2, Value = "测试", IsRequired = false, SelectOptions = """["测试","分析","检查","演示"]""" },
                new Property { OntologyId = reqEmergency.Id,   Name = "状态",       Code = "Status",           PropertyType = "Select", SortOrder = 3, Value = "已批准", IsRequired = false, SelectOptions = """["草稿","评审中","已批准","已验证","已废弃"]""" },
            });

            // --- State 属性 ---
            allProperties.AddRange(new[]
            {
                new Property { OntologyId = stGrounded.Id,  Name = "初始状态", Code = "IsInitial", PropertyType = "Boolean", SortOrder = 1, Value = "true",  IsRequired = false },
                new Property { OntologyId = stGrounded.Id,  Name = "进入动作", Code = "EntryAction",PropertyType = "Text",    SortOrder = 2, Value = "上电自检", IsRequired = false },
                new Property { OntologyId = stTakingOff.Id, Name = "初始状态", Code = "IsInitial", PropertyType = "Boolean", SortOrder = 1, Value = "false", IsRequired = false },
                new Property { OntologyId = stTakingOff.Id, Name = "进入动作", Code = "EntryAction",PropertyType = "Text",    SortOrder = 2, Value = "解锁电机、加速至起飞推力", IsRequired = false },
                new Property { OntologyId = stCruising.Id,  Name = "初始状态", Code = "IsInitial", PropertyType = "Boolean", SortOrder = 1, Value = "false", IsRequired = false },
                new Property { OntologyId = stCruising.Id,  Name = "进入动作", Code = "EntryAction",PropertyType = "Text",    SortOrder = 2, Value = "切入定高/定点模式", IsRequired = false },
                new Property { OntologyId = stLanding.Id,   Name = "初始状态", Code = "IsInitial", PropertyType = "Boolean", SortOrder = 1, Value = "false", IsRequired = false },
                new Property { OntologyId = stLanding.Id,   Name = "进入动作", Code = "EntryAction",PropertyType = "Text",    SortOrder = 2, Value = "减速下降、检测地面", IsRequired = false },
                new Property { OntologyId = stEmergency.Id, Name = "初始状态", Code = "IsInitial", PropertyType = "Boolean", SortOrder = 1, Value = "false", IsRequired = false },
                new Property { OntologyId = stEmergency.Id, Name = "进入动作", Code = "EntryAction",PropertyType = "Text",    SortOrder = 2, Value = "记录异常类型、触发蜂鸣报警", IsRequired = false },
            });

            // --- UseCase 属性 ---
            allProperties.AddRange(new[]
            {
                new Property { OntologyId = ucTakeOff.Id,    Name = "前置条件",   Code = "Precondition",  PropertyType = "Text", SortOrder = 1, Value = "系统自检通过、GPS锁定≥7颗星", IsRequired = false },
                new Property { OntologyId = ucTakeOff.Id,    Name = "后置条件",   Code = "Postcondition", PropertyType = "Text", SortOrder = 2, Value = "无人机进入巡航状态", IsRequired = false },
                new Property { OntologyId = ucTakeOff.Id,    Name = "主参与者",   Code = "PrimaryActor",  PropertyType = "Text", SortOrder = 3, Value = "操作员", IsRequired = false },
                new Property { OntologyId = ucLand.Id,       Name = "前置条件",   Code = "Precondition",  PropertyType = "Text", SortOrder = 1, Value = "无人机在飞行中", IsRequired = false },
                new Property { OntologyId = ucLand.Id,       Name = "后置条件",   Code = "Postcondition", PropertyType = "Text", SortOrder = 2, Value = "无人机触地、电机停转", IsRequired = false },
                new Property { OntologyId = ucLand.Id,       Name = "主参与者",   Code = "PrimaryActor",  PropertyType = "Text", SortOrder = 3, Value = "操作员", IsRequired = false },
                new Property { OntologyId = ucCruise.Id,     Name = "前置条件",   Code = "Precondition",  PropertyType = "Text", SortOrder = 1, Value = "航线已上传、GPS锁定", IsRequired = false },
                new Property { OntologyId = ucCruise.Id,     Name = "后置条件",   Code = "Postcondition", PropertyType = "Text", SortOrder = 2, Value = "完成所有航点", IsRequired = false },
                new Property { OntologyId = ucCruise.Id,     Name = "主参与者",   Code = "PrimaryActor",  PropertyType = "Text", SortOrder = 3, Value = "飞控系统", IsRequired = false },
                new Property { OntologyId = ucReturnHome.Id, Name = "前置条件",   Code = "Precondition",  PropertyType = "Text", SortOrder = 1, Value = "返航点已记录", IsRequired = false },
                new Property { OntologyId = ucReturnHome.Id, Name = "后置条件",   Code = "Postcondition", PropertyType = "Text", SortOrder = 2, Value = "无人机降落至返航点", IsRequired = false },
                new Property { OntologyId = ucReturnHome.Id, Name = "触发条件",   Code = "Trigger",       PropertyType = "Text", SortOrder = 3, Value = "操作员指令 或 通信中断>5s 或 电量<15%", IsRequired = false },
                new Property { OntologyId = ucSurvey.Id,     Name = "前置条件",   Code = "Precondition",  PropertyType = "Text", SortOrder = 1, Value = "航线已规划、相机就绪", IsRequired = false },
                new Property { OntologyId = ucSurvey.Id,     Name = "后置条件",   Code = "Postcondition", PropertyType = "Text", SortOrder = 2, Value = "照片已回传、生成正射影像", IsRequired = false },
                new Property { OntologyId = ucSurvey.Id,     Name = "主参与者",   Code = "PrimaryActor",  PropertyType = "Text", SortOrder = 3, Value = "操作员", IsRequired = false },
            });

            // --- AnalysisCase 属性 ---
            allProperties.AddRange(new[]
            {
                new Property { OntologyId = acWeight.Id,  Name = "分析类型",   Code = "AnalysisType",  PropertyType = "Select", SortOrder = 1, Value = "静力学", IsRequired = true, SelectOptions = """["静力学","动力学","热力学","电磁学","流体力学"]""" },
                new Property { OntologyId = acWeight.Id,  Name = "输入参数",   Code = "InputParams",   PropertyType = "JsonText", SortOrder = 2, Value = """{"components":["Airframe","Payload","PowerSystem"],"targetCG":"center"}""", IsRequired = false },
                new Property { OntologyId = acThrust.Id,  Name = "分析类型",   Code = "AnalysisType",  PropertyType = "Select", SortOrder = 1, Value = "动力学", IsRequired = true, SelectOptions = """["静力学","动力学","热力学","电磁学","流体力学"]""" },
                new Property { OntologyId = acThrust.Id,  Name = "输入参数",   Code = "InputParams",   PropertyType = "JsonText", SortOrder = 2, Value = """{"takeoffMass":"12.5kg","hoverEfficiency":"7g/W","motorCount":4}""", IsRequired = false },
                new Property { OntologyId = acBattery.Id, Name = "分析类型",   Code = "AnalysisType",  PropertyType = "Select", SortOrder = 1, Value = "建模仿真", IsRequired = true, SelectOptions = """["静力学","动力学","热力学","电磁学","流体力学","建模仿真"]""" },
                new Property { OntologyId = acBattery.Id, Name = "输入参数",   Code = "InputParams",   PropertyType = "JsonText", SortOrder = 2, Value = """{"capacity":"20000mAh","cellCount":"6S","avgCurrent":"25A"}""", IsRequired = false },
            });

            // --- ConstraintBlock 属性 ---
            allProperties.AddRange(new[]
            {
                new Property { OntologyId = cbWeight.Id, Name = "约束表达式", Code = "Expression", PropertyType = "JsonText", SortOrder = 1, Value = """{"expr":"m_total <= maxThrust * motorCount * 0.7","unit":"kg"}""", IsRequired = false },
                new Property { OntologyId = cbWeight.Id, Name = "参数列表",   Code = "Parameters", PropertyType = "JsonText", SortOrder = 2, Value = """{"m_total":"总质量","maxThrust":"单电机最大推力","motorCount":"电机数量"}""", IsRequired = false },
                new Property { OntologyId = cbPower.Id,  Name = "约束表达式", Code = "Expression", PropertyType = "JsonText", SortOrder = 1, Value = """{"expr":"sum(componentPower) <= batteryPower * 0.85","unit":"W"}""", IsRequired = false },
                new Property { OntologyId = cbPower.Id,  Name = "参数列表",   Code = "Parameters", PropertyType = "JsonText", SortOrder = 2, Value = """{"componentPower":"各组件功耗数组","batteryPower":"电池持续输出功率"}""", IsRequired = false },
                new Property { OntologyId = cbCG.Id,     Name = "约束表达式", Code = "Expression", PropertyType = "JsonText", SortOrder = 1, Value = """{"expr":"abs(CG_x - TC_x) <= 5 && abs(CG_y - TC_y) <= 5","unit":"mm"}""", IsRequired = false },
                new Property { OntologyId = cbCG.Id,     Name = "参数列表",   Code = "Parameters", PropertyType = "JsonText", SortOrder = 2, Value = """{"CG_x":"重心X坐标","CG_y":"重心Y坐标","TC_x":"推力中心X坐标","TC_y":"推力中心Y坐标"}""", IsRequired = false },
            });

            dbContext.Set<Property>().AddRange(allProperties);
            await dbContext.SaveChangesAsync(cancellationToken);

            // ══════════════════════════════════════════════════════
            // Phase 11: 关系定义
            // ══════════════════════════════════════════════════════

            var allRelations = new List<OntologyRelationship>();

            // --- 包包含关系 (Package containment) ---
            var structureElements = new[] { uavSystem, airframe, propulsion, flightCtrl, navigation, communication, payload, powerSystem, landingGear, groundStation, camera, gpsReceiver,
                                            gpsPort, imuPort, motorCtrlPort, telemetryPort, commandPort, videoPort, powerSupplyPort, powerConsumePort,
                                            ifNav, ifThrust, ifTelemetry, ifPower, ifVideo, ifCommand };
            foreach (var elem in structureElements)
            {
                allRelations.Add(new OntologyRelationship
                {
                    SourceOntologyId = pkgStructure.Id, TargetOntologyId = elem.Id,
                    Name = "包含", DisplayName = "包含", Code = $"PKG_Contains_{elem.Code}",
                    RelationshipType = "Contains", Cardinality = "OneToMany",
                    InverseName = "属于", Description = $"{pkgStructure.Name} 包含 {elem.Name}",
                    SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
                });
            }

            var behaviorElements = new[] { stGrounded, stTakingOff, stCruising, stLanding, stEmergency };
            foreach (var elem in behaviorElements)
            {
                allRelations.Add(new OntologyRelationship
                {
                    SourceOntologyId = pkgBehavior.Id, TargetOntologyId = elem.Id,
                    Name = "包含", DisplayName = "包含", Code = $"PKG_Contains_{elem.Code}",
                    RelationshipType = "Contains", Cardinality = "OneToMany",
                    InverseName = "属于", Description = $"{pkgBehavior.Name} 包含 {elem.Name}",
                    SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
                });
            }

            var requirementElements = new[] { reqMaxSpeed, reqMaxAlt, reqEndurance, reqCommRange, reqPayload, reqSafety, reqGPS, reqEmergency };
            foreach (var elem in requirementElements)
            {
                allRelations.Add(new OntologyRelationship
                {
                    SourceOntologyId = pkgRequirements.Id, TargetOntologyId = elem.Id,
                    Name = "包含", DisplayName = "包含", Code = $"PKG_Contains_{elem.Code}",
                    RelationshipType = "Contains", Cardinality = "OneToMany",
                    InverseName = "属于", Description = $"{pkgRequirements.Name} 包含 {elem.Name}",
                    SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
                });
            }

            var interfaceElements = new[] { ifNav, ifThrust, ifTelemetry, ifPower, ifVideo, ifCommand };
            foreach (var elem in interfaceElements)
            {
                allRelations.Add(new OntologyRelationship
                {
                    SourceOntologyId = pkgInterfaces.Id, TargetOntologyId = elem.Id,
                    Name = "包含", DisplayName = "包含", Code = $"PKG_Contains_{elem.Code}",
                    RelationshipType = "Contains", Cardinality = "OneToMany",
                    InverseName = "属于", Description = $"{pkgInterfaces.Name} 包含 {elem.Name}",
                    SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
                });
            }

            var constraintElements = new[] { cbWeight, cbPower, cbCG };
            foreach (var elem in constraintElements)
            {
                allRelations.Add(new OntologyRelationship
                {
                    SourceOntologyId = pkgConstraints.Id, TargetOntologyId = elem.Id,
                    Name = "包含", DisplayName = "包含", Code = $"PKG_Contains_{elem.Code}",
                    RelationshipType = "Contains", Cardinality = "OneToMany",
                    InverseName = "属于", Description = $"{pkgConstraints.Name} 包含 {elem.Name}",
                    SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
                });
            }

            var viewElements = new[] { ucTakeOff, ucLand, ucCruise, ucReturnHome, ucSurvey, acWeight, acThrust, acBattery };
            foreach (var elem in viewElements)
            {
                allRelations.Add(new OntologyRelationship
                {
                    SourceOntologyId = pkgViews.Id, TargetOntologyId = elem.Id,
                    Name = "包含", DisplayName = "包含", Code = $"PKG_Contains_{elem.Code}",
                    RelationshipType = "Contains", Cardinality = "OneToMany",
                    InverseName = "属于", Description = $"{pkgViews.Name} 包含 {elem.Name}",
                    SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            // --- 部件组合关系 (Part Composition) ---
            var subsystems = new[] { airframe, propulsion, flightCtrl, navigation, communication, payload, powerSystem, landingGear };
            foreach (var sub in subsystems)
            {
                allRelations.Add(new OntologyRelationship
                {
                    SourceOntologyId = uavSystem.Id, TargetOntologyId = sub.Id,
                    Name = "组合", DisplayName = "组合", Code = $"COMP_{uavSystem.Code}_{sub.Code}",
                    RelationshipType = "Composition", Cardinality = "OneToMany",
                    InverseName = "组成部件", Description = $"{uavSystem.Name} 由 {sub.Name} 组合而成",
                    SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
                });
            }

            // --- 端口持有关系 (Port ownership) ---
            var portOwnerships = new (Ontology owner, Ontology port)[]
            {
                (navigation, gpsPort), (navigation, imuPort),
                (propulsion, motorCtrlPort),
                (communication, telemetryPort), (communication, commandPort),
                (payload, videoPort),
                (powerSystem, powerSupplyPort),
                (airframe, powerConsumePort),
            };
            foreach (var (owner, port) in portOwnerships)
            {
                allRelations.Add(new OntologyRelationship
                {
                    SourceOntologyId = owner.Id, TargetOntologyId = port.Id,
                    Name = "持有端口", DisplayName = "持有端口", Code = $"OWN_{owner.Code}_{port.Code}",
                    RelationshipType = "Composition", Cardinality = "OneToMany",
                    InverseName = "所属部件", Description = $"{owner.Name} 持有 {port.Name}",
                    SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
                });
            }

            // --- 端口连接关系 (Connection / Binding) ---
            var connections = new (Ontology src, Ontology tgt, string type, string desc)[]
            {
                (gpsPort, ifNav, "Connect", "GPS端口 → 导航数据接口"),
                (imuPort, ifNav, "Connect", "IMU端口 → 导航数据接口"),
                (motorCtrlPort, ifThrust, "Connect", "电机控制端口 → 推力控制接口"),
                (telemetryPort, ifTelemetry, "Connect", "遥测端口 → 遥测接口"),
                (commandPort, ifCommand, "Connect", "指令端口 → 指令接口"),
                (videoPort, ifVideo, "Connect", "视频流端口 → 视频流接口"),
                (powerSupplyPort, ifPower, "Connect", "供电端口 → 电力分配接口"),
                (powerConsumePort, ifPower, "Connect", "用电端口 → 电力分配接口"),
                (communication, groundStation, "Bind", "通信模块 ↔ 地面站双向绑定"),
            };
            foreach (var (src, tgt, type, desc) in connections)
            {
                allRelations.Add(new OntologyRelationship
                {
                    SourceOntologyId = src.Id, TargetOntologyId = tgt.Id,
                    Name = type == "Connect" ? "连接" : "绑定", DisplayName = type == "Connect" ? "连接" : "绑定",
                    Code = $"{type.ToUpper()}_{src.Code}_{tgt.Code}",
                    RelationshipType = type, Cardinality = "OneToOne",
                    InverseName = type == "Connect" ? "被连接" : "被绑定",
                    Description = desc, SortOrder = 1, IsEnabled = true,
                    CreatedAt = now, UpdatedAt = now
                });
            }

            // --- 需求满足/追溯/细化/验证关系 ---
            var satisfyMappings = new (Ontology req, Ontology target)[]
            {
                (reqMaxSpeed, propulsion), (reqMaxAlt, propulsion),
                (reqEndurance, powerSystem), (reqCommRange, communication),
                (reqPayload, payload), (reqSafety, flightCtrl),
                (reqGPS, gpsReceiver), (reqEmergency, flightCtrl),
            };
            foreach (var (req, target) in satisfyMappings)
            {
                allRelations.Add(new OntologyRelationship
                {
                    SourceOntologyId = req.Id, TargetOntologyId = target.Id,
                    Name = "满足", DisplayName = "满足", Code = $"SAT_{req.Code}_{target.Code}",
                    RelationshipType = "Satisfy", Cardinality = "ManyToMany",
                    InverseName = "被满足", Description = $"{req.Name} 由 {target.Name} 满足",
                    SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
                });
            }

            // --- 用例分配关系 (Allocation use cases to structure) ---
            var allocations = new (Ontology uc, Ontology target)[]
            {
                (ucTakeOff, flightCtrl), (ucLand, flightCtrl),
                (ucCruise, flightCtrl), (ucReturnHome, flightCtrl),
                (ucSurvey, payload),
            };
            foreach (var (uc, target) in allocations)
            {
                allRelations.Add(new OntologyRelationship
                {
                    SourceOntologyId = uc.Id, TargetOntologyId = target.Id,
                    Name = "分配", DisplayName = "分配", Code = $"ALLOC_{uc.Code}_{target.Code}",
                    RelationshipType = "Allocate", Cardinality = "ManyToMany",
                    InverseName = "被分配", Description = $"{uc.Name} 分配至 {target.Name} 执行",
                    SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
                });
            }

            // --- 状态转换关系 (Transition) ---
            var transitions = new (Ontology from, Ontology to, string trigger)[]
            {
                (stGrounded, stTakingOff, "收到起飞指令"),
                (stTakingOff, stCruising, "达到目标高度"),
                (stCruising, stLanding, "收到降落指令"),
                (stLanding, stGrounded, "触地检测"),
                (stCruising, stEmergency, "通信中断>5s 或 电量<10%"),
                (stEmergency, stLanding, "异常恢复 或 强制降落"),
            };
            foreach (var (from, to, trigger) in transitions)
            {
                allRelations.Add(new OntologyRelationship
                {
                    SourceOntologyId = from.Id, TargetOntologyId = to.Id,
                    Name = "转换", DisplayName = "转换", Code = $"TR_{from.Code}_TO_{to.Code}",
                    RelationshipType = "Transition", Cardinality = "OneToOne",
                    InverseName = "反向转换", Description = $"[{trigger}] {from.Name} → {to.Name}",
                    SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
                });
            }

            // --- 约束块绑定到部件 ---
            allRelations.Add(new OntologyRelationship
            {
                SourceOntologyId = cbWeight.Id, TargetOntologyId = uavSystem.Id,
                Name = "绑定", DisplayName = "绑定", Code = "BIND_CBWeight_UAV",
                RelationshipType = "Bind", Cardinality = "OneToOne",
                InverseName = "被绑定", Description = "总重约束绑定至无人机系统",
                SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
            });
            allRelations.Add(new OntologyRelationship
            {
                SourceOntologyId = cbPower.Id, TargetOntologyId = powerSystem.Id,
                Name = "绑定", DisplayName = "绑定", Code = "BIND_CBPower_PowerSys",
                RelationshipType = "Bind", Cardinality = "OneToOne",
                InverseName = "被绑定", Description = "功耗预算约束绑定至电源系统",
                SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
            });
            allRelations.Add(new OntologyRelationship
            {
                SourceOntologyId = cbCG.Id, TargetOntologyId = airframe.Id,
                Name = "绑定", DisplayName = "绑定", Code = "BIND_CBCG_Airframe",
                RelationshipType = "Bind", Cardinality = "OneToOne",
                InverseName = "被绑定", Description = "重心约束绑定至机身结构",
                SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
            });

            // --- 分析案例追溯需求 ---
            allRelations.Add(new OntologyRelationship
            {
                SourceOntologyId = acWeight.Id, TargetOntologyId = reqPayload.Id,
                Name = "追溯", DisplayName = "追溯", Code = "TRACE_ACWeight_REQPayload",
                RelationshipType = "Trace", Cardinality = "OneToOne",
                InverseName = "被追溯", Description = "重量平衡分析追溯至载荷需求",
                SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
            });
            allRelations.Add(new OntologyRelationship
            {
                SourceOntologyId = acThrust.Id, TargetOntologyId = reqMaxSpeed.Id,
                Name = "追溯", DisplayName = "追溯", Code = "TRACE_ACThrust_REQSpeed",
                RelationshipType = "Trace", Cardinality = "OneToOne",
                InverseName = "被追溯", Description = "推力计算分析追溯至最大速度需求",
                SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
            });
            allRelations.Add(new OntologyRelationship
            {
                SourceOntologyId = acBattery.Id, TargetOntologyId = reqEndurance.Id,
                Name = "追溯", DisplayName = "追溯", Code = "TRACE_ACBattery_REQEndurance",
                RelationshipType = "Trace", Cardinality = "OneToOne",
                InverseName = "被追溯", Description = "电池寿命模型追溯至续航时间需求",
                SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
            });

            // --- 需求间细化关系 (Refine) ---
            allRelations.Add(new OntologyRelationship
            {
                SourceOntologyId = reqSafety.Id, TargetOntologyId = reqEmergency.Id,
                Name = "细化", DisplayName = "细化", Code = "REFINE_REQSafety_REQEmergency",
                RelationshipType = "Refine", Cardinality = "OneToOne",
                InverseName = "被细化", Description = "安全降落需求细化出紧急返航需求",
                SortOrder = 1, IsEnabled = true, CreatedAt = now, UpdatedAt = now
            });

            dbContext.Set<OntologyRelationship>().AddRange(allRelations);
            await dbContext.SaveChangesAsync(cancellationToken);

            // ══════════════════════════════════════════════════════
            // Phase 12: 对象行为定义 (M2)
            // ══════════════════════════════════════════════════════

            var behaviors = new List<ObjectBehavior>
            {
                new()
                {
                    OntologyId = flightCtrl.Id, Name = "解锁电机", Description = "执行解锁序列：校验GPS→解锁ESC→确认转速",
                    ActionType = ActionType.Custom, ApiUrl = "/api/flight/unlock",
                    ConfirmMessage = "确认解锁所有电机？请确保周围安全！",
                    IsEnabled = true, SortOrder = 1, CreatedAt = now, UpdatedAt = now
                },
                new()
                {
                    OntologyId = flightCtrl.Id, Name = "紧急停止", Description = "立即切断所有电机输出，进入安全状态",
                    ActionType = ActionType.Custom, ApiUrl = "/api/flight/emergency-stop",
                    ConfirmMessage = "紧急停止将立即切断动力！确定执行？",
                    IsEnabled = true, SortOrder = 2, CreatedAt = now, UpdatedAt = now
                },
                new()
                {
                    OntologyId = flightCtrl.Id, Name = "切换飞行模式", Description = "切换飞行模式：自稳/定高/定点/任务",
                    ActionType = ActionType.Navigate, ApiUrl = "/api/flight/mode-switch",
                    ConfirmMessage = "切换飞行模式可能导致飞行姿态变化，确认切换？",
                    IsEnabled = true, SortOrder = 3, CreatedAt = now, UpdatedAt = now
                },
                new()
                {
                    OntologyId = communication.Id, Name = "遥测数据上报", Description = "定时上报飞行遥测数据至地面站",
                    ActionType = ActionType.Submit, ApiUrl = "/api/telemetry/report",
                    IsEnabled = true, SortOrder = 1, CreatedAt = now, UpdatedAt = now
                },
                new()
                {
                    OntologyId = groundStation.Id, Name = "上传航线", Description = "将规划好的航线文件上传至无人机",
                    ActionType = ActionType.Submit, ApiUrl = "/api/mission/upload",
                    ConfirmMessage = "上传航线将覆盖当前任务，确认上传？",
                    IsEnabled = true, SortOrder = 1, CreatedAt = now, UpdatedAt = now
                },
            };

            dbContext.Set<ObjectBehavior>().AddRange(behaviors);
            await dbContext.SaveChangesAsync(cancellationToken);

            // ══════════════════════════════════════════════════════
            // Phase 13: 规则定义 (M3)
            // ══════════════════════════════════════════════════════

            var rules = new List<OntologyRule>
            {
                new()
                {
                    Name = "起飞前自检规则", Description = "起飞前必须通过GPS锁定、IMU校准、电量检查",
                    RuleCondition = """{"GPSFix":{"op":"gte","value":7},"IMUCalibrated":{"op":"eq","value":true},"BatteryPct":{"op":"gte","value":30}}""",
                    RuleExpression = "all(conditions) => unlockAllowed = true",
                    IsEnabled = true, SortOrder = 1, CreatedAt = now, UpdatedAt = now
                },
                new()
                {
                    Name = "低电量自动返航规则", Description = "电量低于15%时自动触发返航",
                    RuleCondition = """{"BatteryPct":{"op":"lt","value":15},"FlightMode":{"op":"neq","value":"Landing"}}""",
                    RuleExpression = "trigger(ReturnHome) if BatteryPct < 15%",
                    IsEnabled = true, SortOrder = 2, CreatedAt = now, UpdatedAt = now
                },
                new()
                {
                    Name = "通信中断保护规则", Description = "通信中断超过5秒触发紧急返航",
                    RuleCondition = """{"LastHeartbeat":{"op":"gt","value":5000},"FlightMode":{"op":"neq","value":"Emergency"}}""",
                    RuleExpression = "trigger(EmergencyReturn) if heartbeat_timeout > 5000ms",
                    IsEnabled = true, SortOrder = 3, CreatedAt = now, UpdatedAt = now
                },
            };

            dbContext.Set<OntologyRule>().AddRange(rules);
            await dbContext.SaveChangesAsync(cancellationToken);

            var totalCount = 56 + allRelations.Count + behaviors.Count + rules.Count;
            Console.WriteLine($"[SeedData] SysML V2 UAV 体系种子数据全部创建完成，共 {totalCount} 条（本体56 + 关系{allRelations.Count} + 行为{behaviors.Count} + 规则{rules.Count}）");
            Log.Information("[SeedData] SysML V2 UAV 体系种子数据全部创建完成（本体56 + 关系{RelCount} + 行为{BehCount} + 规则{RuleCount}）",
                allRelations.Count, behaviors.Count, rules.Count);
        }

        #endregion
    }
}
