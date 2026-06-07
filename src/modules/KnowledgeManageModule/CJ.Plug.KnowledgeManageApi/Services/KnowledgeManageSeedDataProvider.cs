using CJ.Plug.Models.Contracts;
using CJ.Plug.Models.Knowledge;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace CJ.Plug.KnowledgeManageApi.Services
{
    /// <summary>
    /// 知识管理模块种子数据提供者
    /// 预设 3 个知识库及其多级文件夹结构
    /// </summary>
    public class KnowledgeManageSeedDataProvider : ISeedDataProvider
    {
        public string Name => "知识管理模块种子数据";
        public int Order => 130;

        public async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        {
            var dbContext = serviceProvider.GetRequiredService<MainDbContext>();

            await SeedKnowledgeBasesAsync(dbContext, cancellationToken);
        }

        #region 知识库与文件夹

        private static async Task SeedKnowledgeBasesAsync(MainDbContext dbContext, CancellationToken cancellationToken)
        {
            var now = new DateTime(2026, 6, 1, 0, 0, 0);

            // 检查是否已有知识库种子数据
            var existingBases = await dbContext.Set<KnowledgeBase>()
                .AnyAsync(cancellationToken);
            
            KnowledgeBase designBase, processBase, maintenanceBase;

            if (existingBases)
            {
                Console.WriteLine("[SeedData] KnowledgeManage 知识库已存在，跳过知识库和文件夹创建");
                designBase = await dbContext.Set<KnowledgeBase>()
                    .FirstAsync(b => b.Name == "工程设计知识库", cancellationToken);
                processBase = await dbContext.Set<KnowledgeBase>()
                    .FirstAsync(b => b.Name == "工艺流程知识库", cancellationToken);
                maintenanceBase = await dbContext.Set<KnowledgeBase>()
                    .FirstAsync(b => b.Name == "设备维护知识库", cancellationToken);
            }
            else
            {

            // ========================================
            // 知识库 1：工程设计知识库
            // ========================================
            designBase = new KnowledgeBase
            {
                Name = "工程设计知识库",
                Description = "涵盖工程设计领域的标准规范、设计方法、常见问题及最佳实践。",
                Order = 1,
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.Set<KnowledgeBase>().Add(designBase);
            await dbContext.SaveChangesAsync(cancellationToken);
            Log.Information("[SeedData] 知识库 {Name} 已创建", designBase.Name);

            // 一级文件夹
            var designFolder1 = new KnowledgeFolder
            {
                BaseId = designBase.Id, Name = "设计规范", ParentId = null,
                Description = "国家及行业设计标准规范汇编", Icon = "description", Order = 1,
                CreatedAt = now, UpdatedAt = now
            };
            var designFolder2 = new KnowledgeFolder
            {
                BaseId = designBase.Id, Name = "设计方法", ParentId = null,
                Description = "常用设计方法论与流程指南", Icon = "architecture", Order = 2,
                CreatedAt = now, UpdatedAt = now
            };
            var designFolder3 = new KnowledgeFolder
            {
                BaseId = designBase.Id, Name = "常见问题", ParentId = null,
                Description = "设计过程中常见问题与解决方案", Icon = "help_outline", Order = 3,
                CreatedAt = now, UpdatedAt = now
            };
            dbContext.Set<KnowledgeFolder>().AddRange(designFolder1, designFolder2, designFolder3);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 二级文件夹 - 设计规范下
            dbContext.Set<KnowledgeFolder>().AddRange(
                new KnowledgeFolder
                {
                    BaseId = designBase.Id, Name = "国家标准", ParentId = designFolder1.Id,
                    Description = "GB/T 系列国家标准", Icon = "flag", Order = 1,
                    CreatedAt = now, UpdatedAt = now
                },
                new KnowledgeFolder
                {
                    BaseId = designBase.Id, Name = "行业标准", ParentId = designFolder1.Id,
                    Description = "各行业协会发布的标准", Icon = "corporate_fare", Order = 2,
                    CreatedAt = now, UpdatedAt = now
                },
                new KnowledgeFolder
                {
                    BaseId = designBase.Id, Name = "企业内部标准", ParentId = designFolder1.Id,
                    Description = "公司内部设计规范与准则", Icon = "business", Order = 3,
                    CreatedAt = now, UpdatedAt = now
                }
            );

            // 二级文件夹 - 设计方法下
            dbContext.Set<KnowledgeFolder>().AddRange(
                new KnowledgeFolder
                {
                    BaseId = designBase.Id, Name = "结构设计", ParentId = designFolder2.Id,
                    Description = "结构力学与强度设计方法", Icon = "foundation", Order = 1,
                    CreatedAt = now, UpdatedAt = now
                },
                new KnowledgeFolder
                {
                    BaseId = designBase.Id, Name = "流体设计", ParentId = designFolder2.Id,
                    Description = "流体动力学与管道设计", Icon = "water_drop", Order = 2,
                    CreatedAt = now, UpdatedAt = now
                },
                new KnowledgeFolder
                {
                    BaseId = designBase.Id, Name = "电气设计", ParentId = designFolder2.Id,
                    Description = "电气系统与控制回路设计", Icon = "bolt", Order = 3,
                    CreatedAt = now, UpdatedAt = now
                }
            );
            await dbContext.SaveChangesAsync(cancellationToken);
            Log.Information("[SeedData] 工程设计知识库文件夹创建完成");

            // ========================================
            // 知识库 2：工艺流程知识库
            // ========================================
            processBase = new KnowledgeBase
            {
                Name = "工艺流程知识库",
                Description = "生产工艺流程、操作规程、质量控制等工艺相关知识。",
                Order = 2,
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.Set<KnowledgeBase>().Add(processBase);
            await dbContext.SaveChangesAsync(cancellationToken);
            Log.Information("[SeedData] 知识库 {Name} 已创建", processBase.Name);

            // 一级文件夹
            var processFolder1 = new KnowledgeFolder
            {
                BaseId = processBase.Id, Name = "加工工艺", ParentId = null,
                Description = "各类加工工艺方法与参数", Icon = "precision_manufacturing", Order = 1,
                CreatedAt = now, UpdatedAt = now
            };
            var processFolder2 = new KnowledgeFolder
            {
                BaseId = processBase.Id, Name = "装配工艺", ParentId = null,
                Description = "产品装配流程与技术要求", Icon = "build", Order = 2,
                CreatedAt = now, UpdatedAt = now
            };
            var processFolder3 = new KnowledgeFolder
            {
                BaseId = processBase.Id, Name = "质量控制", ParentId = null,
                Description = "质量检验标准与控制方法", Icon = "verified", Order = 3,
                CreatedAt = now, UpdatedAt = now
            };
            var processFolder4 = new KnowledgeFolder
            {
                BaseId = processBase.Id, Name = "安全规程", ParentId = null,
                Description = "生产安全操作规程与注意事项", Icon = "security", Order = 4,
                CreatedAt = now, UpdatedAt = now
            };
            dbContext.Set<KnowledgeFolder>().AddRange(processFolder1, processFolder2, processFolder3, processFolder4);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 二级文件夹 - 加工工艺下
            dbContext.Set<KnowledgeFolder>().AddRange(
                new KnowledgeFolder
                {
                    BaseId = processBase.Id, Name = "数控加工", ParentId = processFolder1.Id,
                    Description = "CNC 数控机床加工工艺", Icon = "settings", Order = 1,
                    CreatedAt = now, UpdatedAt = now
                },
                new KnowledgeFolder
                {
                    BaseId = processBase.Id, Name = "焊接工艺", ParentId = processFolder1.Id,
                    Description = "各类焊接方法与工艺参数", Icon = "local_fire_department", Order = 2,
                    CreatedAt = now, UpdatedAt = now
                },
                new KnowledgeFolder
                {
                    BaseId = processBase.Id, Name = "热处理工艺", ParentId = processFolder1.Id,
                    Description = "金属热处理工艺规范", Icon = "thermostat", Order = 3,
                    CreatedAt = now, UpdatedAt = now
                }
            );

            // 二级文件夹 - 质量控制下
            dbContext.Set<KnowledgeFolder>().AddRange(
                new KnowledgeFolder
                {
                    BaseId = processBase.Id, Name = "无损检测", ParentId = processFolder3.Id,
                    Description = "NDT 无损检测方法与标准", Icon = "search", Order = 1,
                    CreatedAt = now, UpdatedAt = now
                },
                new KnowledgeFolder
                {
                    BaseId = processBase.Id, Name = "尺寸检测", ParentId = processFolder3.Id,
                    Description = "尺寸精度检测与量具使用", Icon = "straighten", Order = 2,
                    CreatedAt = now, UpdatedAt = now
                }
            );
            await dbContext.SaveChangesAsync(cancellationToken);
            Log.Information("[SeedData] 工艺流程知识库文件夹创建完成");

            // ========================================
            // 知识库 3：设备维护知识库
            // ========================================
            maintenanceBase = new KnowledgeBase
            {
                Name = "设备维护知识库",
                Description = "设备维护保养、故障诊断、维修手册等设备管理知识。",
                Order = 3,
                CreatedAt = now,
                UpdatedAt = now
            };
            dbContext.Set<KnowledgeBase>().Add(maintenanceBase);
            await dbContext.SaveChangesAsync(cancellationToken);
            Log.Information("[SeedData] 知识库 {Name} 已创建", maintenanceBase.Name);

            // 一级文件夹
            var maintFolder1 = new KnowledgeFolder
            {
                BaseId = maintenanceBase.Id, Name = "预防性维护", ParentId = null,
                Description = "设备定期维护保养计划与规程", Icon = "event_repeat", Order = 1,
                CreatedAt = now, UpdatedAt = now
            };
            var maintFolder2 = new KnowledgeFolder
            {
                BaseId = maintenanceBase.Id, Name = "故障诊断", ParentId = null,
                Description = "设备故障现象分析与诊断方法", Icon = "troubleshoot", Order = 2,
                CreatedAt = now, UpdatedAt = now
            };
            var maintFolder3 = new KnowledgeFolder
            {
                BaseId = maintenanceBase.Id, Name = "维修手册", ParentId = null,
                Description = "各类设备维修操作手册", Icon = "menu_book", Order = 3,
                CreatedAt = now, UpdatedAt = now
            };
            dbContext.Set<KnowledgeFolder>().AddRange(maintFolder1, maintFolder2, maintFolder3);
            await dbContext.SaveChangesAsync(cancellationToken);

            // 二级文件夹 - 预防性维护下
            dbContext.Set<KnowledgeFolder>().AddRange(
                new KnowledgeFolder
                {
                    BaseId = maintenanceBase.Id, Name = "日常点检", ParentId = maintFolder1.Id,
                    Description = "设备日常巡检项目与标准", Icon = "checklist", Order = 1,
                    CreatedAt = now, UpdatedAt = now
                },
                new KnowledgeFolder
                {
                    BaseId = maintenanceBase.Id, Name = "定期保养", ParentId = maintFolder1.Id,
                    Description = "周/月/季/年度保养计划", Icon = "calendar_month", Order = 2,
                    CreatedAt = now, UpdatedAt = now
                },
                new KnowledgeFolder
                {
                    BaseId = maintenanceBase.Id, Name = "润滑管理", ParentId = maintFolder1.Id,
                    Description = "润滑油品选择与加注规范", Icon = "oil_barrel", Order = 3,
                    CreatedAt = now, UpdatedAt = now
                }
            );

            // 二级文件夹 - 故障诊断下
            dbContext.Set<KnowledgeFolder>().AddRange(
                new KnowledgeFolder
                {
                    BaseId = maintenanceBase.Id, Name = "机械故障", ParentId = maintFolder2.Id,
                    Description = "机械传动、轴承、齿轮等故障", Icon = "gear", Order = 1,
                    CreatedAt = now, UpdatedAt = now
                },
                new KnowledgeFolder
                {
                    BaseId = maintenanceBase.Id, Name = "电气故障", ParentId = maintFolder2.Id,
                    Description = "电机、变频器、传感器等故障", Icon = "electric_bolt", Order = 2,
                    CreatedAt = now, UpdatedAt = now
                },
                new KnowledgeFolder
                {
                    BaseId = maintenanceBase.Id, Name = "液压故障", ParentId = maintFolder2.Id,
                    Description = "液压系统压力、泄漏等故障", Icon = "opacity", Order = 3,
                    CreatedAt = now, UpdatedAt = now
                }
            );

            // 三级文件夹 - 机械故障下
            dbContext.Set<KnowledgeFolder>().AddRange(
                new KnowledgeFolder
                {
                    BaseId = maintenanceBase.Id, Name = "轴承故障", ParentId = null,
                    Description = "轴承磨损、异响、温升等故障分析",
                    Icon = "circle", Order = 1,
                    CreatedAt = now, UpdatedAt = now
                }
            );
            await dbContext.SaveChangesAsync(cancellationToken);

            // 手动设置三级文件夹的 ParentId（需要先获取二级文件夹的 Id）
            var mechanicalFolder = await dbContext.Set<KnowledgeFolder>()
                .FirstAsync(f => f.BaseId == maintenanceBase.Id && f.Name == "机械故障", cancellationToken);
            var bearingFolder = await dbContext.Set<KnowledgeFolder>()
                .FirstAsync(f => f.BaseId == maintenanceBase.Id && f.Name == "轴承故障", cancellationToken);
            bearingFolder.ParentId = mechanicalFolder.Id;
            await dbContext.SaveChangesAsync(cancellationToken);

            Log.Information("[SeedData] 设备维护知识库文件夹创建完成");
            }

            // 知识条目（独立于知识库创建，始终检查并补插）
            var hasItems = await dbContext.Set<KnowledgeItem>().AnyAsync(cancellationToken);
            if (!hasItems)
            {
                await SeedKnowledgeItemsAsync(dbContext, designBase, processBase, maintenanceBase, now, cancellationToken);
                Console.WriteLine("[SeedData] 知识管理模块种子数据全部创建完成：3 个知识库，多级文件夹结构 + 知识条目");
            }
            else
            {
                Console.WriteLine("[SeedData] 知识条目已存在，跳过");
            }
        }

        private static async Task SeedKnowledgeItemsAsync(
            MainDbContext dbContext,
            KnowledgeBase designBase,
            KnowledgeBase processBase,
            KnowledgeBase maintenanceBase,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var allFolders = await dbContext.Set<KnowledgeFolder>()
                .Include(f => f.Base)
                .Include(f => f.Parent)
                .ToListAsync(cancellationToken);

            KnowledgeFolder GetFolder(string baseName, string folderName)
                => allFolders.First(f => f.Base?.Name == baseName && f.Name == folderName)
                   ?? throw new InvalidOperationException($"找不到文件夹: {baseName}/{folderName}");

            KnowledgeFolder GetChildFolder(string baseName, string parentName, string folderName)
                => allFolders.First(f => f.Base?.Name == baseName && f.Parent?.Name == parentName && f.Name == folderName)
                   ?? throw new InvalidOperationException($"找不到文件夹: {baseName}/{parentName}/{folderName}");

            var items = new List<KnowledgeItem>();

            // ========================================
            // 工程设计知识库 - 条目
            // ========================================
            int order = 0;

            // 设计规范 > 国家标准
            var gbFolder = GetChildFolder("工程设计知识库", "设计规范", "国家标准");
            items.Add(new KnowledgeItem
            {
                FolderId = gbFolder.Id,
                Title = "GB/T 12345-2024 通用设计规范概述",
                Content = @"**适用范围**：适用于工业装备设计的通用技术要求。

**核心要点**：
1. 设计输入必须包含完整的工况参数和环境条件
2. 安全系数按照载荷类型分档取值：
   - 静载荷：安全系数 ≥ 1.5
   - 动载荷：安全系数 ≥ 2.0
   - 冲击载荷：安全系数 ≥ 3.0
3. 关键承力部件必须进行有限元分析校核
4. 设计输出应包含 BOM 清单、工程图纸和技术规格书",
                Tags = "国家标准,设计规范,安全系数,有限元分析",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });
            items.Add(new KnowledgeItem
            {
                FolderId = gbFolder.Id,
                Title = "GB/T 67890-2024 结构件焊接设计规范要点",
                Content = @"**适用对象**：碳钢及低合金钢焊接结构件。

**关键规定**：
1. 焊缝等级划分：Ⅰ级（关键承力）、Ⅱ级（一般承力）、Ⅲ级（非承力）
2. 角焊缝焊脚尺寸 ≥ 0.7×较薄板厚，且不小于 3mm
3. 对接焊缝需开坡口时，坡口角度 60°±5°
4. 相邻焊缝间距 ≥ 100mm 或 4 倍板厚（取大值）

**检验要求**：Ⅰ级焊缝 100% UT，Ⅱ级焊缝 20% UT 抽检",
                Tags = "国家标准,焊接规范,焊缝,结构设计",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 设计规范 > 行业标准
            var hyFolder = GetChildFolder("工程设计知识库", "设计规范", "行业标准");
            items.Add(new KnowledgeItem
            {
                FolderId = hyFolder.Id,
                Title = "SH/T 3015-2024 化工设备设计技术条件",
                Content = @"**标准概述**：石油化工静设备设计通用技术条件。

**设计参数**：
| 参数 | 要求 |
|------|------|
| 设计压力 | ≥ 1.1 倍最高工作压力 |
| 设计温度 | 最高工作温度 + 30°C |
| 腐蚀裕量 | ≥ 1.5mm（碳钢） |

**材料选择原则**：
- 临氢工况优先选用 Cr-Mo 钢
- 酸性介质选用 316L 不锈钢及以上
- 低温工况（< -20°C）需做低温冲击试验",
                Tags = "行业标准,化工设备,设计条件,材料选择",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 设计规范 > 企业内部标准
            var qyFolder = GetChildFolder("工程设计知识库", "设计规范", "企业内部标准");
            items.Add(new KnowledgeItem
            {
                FolderId = qyFolder.Id,
                Title = "CJP-QS-001 设计评审标准流程",
                Content = @"**评审阶段与参与方**：

| 阶段 | 参与方 | 输出物 |
|------|--------|--------|
| 方案评审 | 技术负责人 + 主任工程师 | 方案评审纪要 |
| 详细设计评审 | 各专业负责人 | 详细设计评审表 |
| 工艺会签 | 工艺工程师 | 会签意见单 |
| 最终评审 | 总工程师 | 设计批准书 |

**否决条件**：
- 安全系数不达标直接否决
- 缺少关键计算报告不予通过
- 材料选型未经确认不得进入下一阶段",
                Tags = "企业内部标准,设计评审,流程管理,质量管理",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 设计方法 > 结构设计
            var jgFolder = GetChildFolder("工程设计知识库", "设计方法", "结构设计");
            items.Add(new KnowledgeItem
            {
                FolderId = jgFolder.Id,
                Title = "钢结构强度设计方法与实例",
                Content = @"**设计流程**：
1. 建立力学模型 → 2. 载荷计算 → 3. 内力分析 → 4. 截面验算 → 5. 节点设计

**常用验算公式**：
- 抗弯强度：σ = M / (γx × Wnx) ≤ f
- 抗剪强度：τ = VS / (Itw) ≤ fv
- 折算应力：√(σ² + 3τ²) ≤ 1.1f

**设计实例**：
某钢平台主梁，跨度 6m，均布荷载 8kN/m²。
选用 HN350×175，验算结果：σ_max = 187MPa < f = 215MPa，满足。

**注意事项**：
- H 型钢强轴布置于受弯方向
- 悬臂梁挠度限值 L/250
- 简支梁挠度限值 L/400",
                Tags = "结构设计,钢结构,强度计算,设计实例",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });
            items.Add(new KnowledgeItem
            {
                FolderId = jgFolder.Id,
                Title = "混凝土结构配筋设计原则",
                Content = @"**基本规定**：
1. 最小配筋率 ρ_min = Max(0.2%, 0.45ft/fy)
2. 最大配筋率 ρ_max = 0.025（适筋梁上限）
3. 梁纵筋直径 ≥ 12mm，板纵筋直径 ≥ 8mm
4. 受弯构件受压区高度 x ≤ 0.55h0

**配筋构造要求**：
- 梁顶筋间距 ≥ 30mm 且 ≥ 1.5d
- 梁底筋间距 ≥ 25mm 且 ≥ d
- 箍筋间距 ≤ 15d（柱）、≤ 200mm（梁）

**抗裂措施**：
优先采用小直径密配筋方案控制裂缝宽度 ≤ 0.3mm",
                Tags = "结构设计,混凝土,配筋,构造要求",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 设计方法 > 电气设计
            var dqFolder = GetChildFolder("工程设计知识库", "设计方法", "电气设计");
            items.Add(new KnowledgeItem
            {
                FolderId = dqFolder.Id,
                Title = "低压配电系统设计指南",
                Content = @"**系统架构**：
TN-S 系统（三相五线制），额定电压 380V/220V，频率 50Hz。

**负荷分级**：
| 级别 | 定义 | 供电要求 |
|------|------|----------|
| 一级 | 中断造成重大损失 | 双电源自动切换 |
| 二级 | 中断造成较大损失 | 双回路供电 |
| 三级 | 其他负荷 | 单回路供电 |

**电缆选型**：
- 动力电缆：YJV-0.6/1kV 铜芯交联聚乙烯绝缘
- 控制电缆：KVV-450/750V 铜芯聚氯乙烯绝缘
- 消防线路：NH-YJV 耐火型，敷设于耐火桥架

**保护配置**：
断路器 + 热继电器（电动机回路），漏电保护器 IΔn ≤ 30mA（插座回路）。",
                Tags = "电气设计,配电系统,电缆选型,保护配置",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 常见问题
            var cjFolder = GetFolder("工程设计知识库", "常见问题");
            items.Add(new KnowledgeItem
            {
                FolderId = cjFolder.Id,
                Title = "离心泵选型常见问题与对策",
                Content = @"**问题 1：泵的汽蚀余量不足**
现象：泵运行时有明显振动和噪音，出口压力波动。
原因：安装高度过高或吸入管路阻力过大。
对策：降低安装高度，增大吸入管径，或选用低 NPSHr 泵型。

**问题 2：电机功率选型过大**
现象：电机长期在低负载率（< 40%）下运行，能耗浪费。
对策：按轴功率 × 1.15~1.25 的安全系数选型，避免过大裕量。

**问题 3：并联泵流量分配不均**
现象：相同型号泵并联运行但出口流量差异 > 10%。
原因：管路阻力特性不一致。
对策：调整出口阀门开度，或在汇管前增加等长管段。

**选型流程简图**：
确定工况(Q,H) → 计算装置汽蚀余量 NPSHa → 初选泵型 → 校核 NPSHa > NPSHr + 0.5m → 确定电机功率",
                Tags = "常见问题,离心泵,选型,汽蚀,电机",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // ========================================
            // 工艺流程知识库 - 条目
            // ========================================
            order = 0;

            // 加工工艺 > 数控加工
            var skFolder = GetChildFolder("工艺流程知识库", "加工工艺", "数控加工");
            items.Add(new KnowledgeItem
            {
                FolderId = skFolder.Id,
                Title = "五轴加工中心编程规范",
                Content = @"**编程前准备**：
1. 确认机床行程范围（X/Y/Z/A/C 轴行程）
2. 确认刀具库可用刀位及刀柄类型
3. 获取毛坯尺寸和装夹方案

**编程要点**：
- 粗加工：选用大直径端铣刀，切深 2-4mm，切宽 60%-80% 刀具直径
- 精加工：选用球头铣刀，行距 0.1-0.3mm，转速 6000-12000rpm
- 刀轴控制：避免刀轴剧烈变化，保持进给速度稳定

**安全规范**：
- 程序起始加入 G90 G54 G17 坐标系初始化
- 每次换刀后验证刀长补偿
- 快速移动时 Z 轴先抬到安全高度（≥ 50mm）

**后处理**：
统一使用 Siemens 840D 后处理器，NC 文件扩展名 .mpf",
                Tags = "数控加工,五轴加工,编程规范,后处理",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });
            items.Add(new KnowledgeItem
            {
                FolderId = skFolder.Id,
                Title = "刀具选择与切削参数参考表",
                Content = @"**常用刀具切削参数（加工钢件 HRC 20-35）**：

| 刀具类型 | 材质 | 线速度(m/min) | 每齿进给(mm/z) | 切深(mm) |
|----------|------|--------------|----------------|---------|
| 面铣刀 | 硬质合金 | 150-250 | 0.12-0.25 | 1-3 |
| 立铣刀 φ10 | 涂层硬质合金 | 80-150 | 0.03-0.08 | 0.5-1.5D |
| 钻头 φ8 | 高速钢 | 20-35 | 0.1-0.2mm/r | - |
| 丝锥 M10 | 高速钢 | 8-15 | - | - |

**刀具寿命参考**：硬质合金铣刀连续切削 60-90min 建议刃磨。

**冷却液选择**：
- 钢件：乳化液浓度 5%-8%
- 铝合金：专用切削液或煤油
- 铸铁：干切削或压缩空气冷却",
                Tags = "数控加工,刀具选择,切削参数,冷却液",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 加工工艺 > 焊接工艺
            var hjFolder = GetChildFolder("工艺流程知识库", "加工工艺", "焊接工艺");
            items.Add(new KnowledgeItem
            {
                FolderId = hjFolder.Id,
                Title = "TIG 焊接工艺规程",
                Content = @"**适用范围**：不锈钢、钛合金、铝合金薄板焊接（厚度 1-6mm）。

**焊前准备**：
1. 坡口清理：机械打磨至金属光泽，丙酮擦拭
2. 保护气体：纯氩气，纯度 ≥ 99.99%，流量 8-15 L/min
3. 钨极：铈钨极 φ2.4mm，尖端磨成 30° 锥角

**工艺参数**：

| 板厚(mm) | 电流(A) | 焊速(mm/min) | 氩气流量(L/min) | 焊丝直径(mm) |
|----------|---------|-------------|----------------|-------------|
| 1.0 | 40-60 | 80-120 | 6-8 | 1.6 |
| 2.0 | 80-120 | 60-100 | 8-10 | 2.0 |
| 3.0 | 120-160 | 50-80 | 10-12 | 2.4 |
| 4.0 | 160-200 | 40-60 | 12-14 | 2.4 |

**质量要求**：焊缝表面呈银白色或淡黄色（合格），蓝色需打磨重焊，灰色直接报废。",
                Tags = "焊接工艺,TIG焊,工艺参数,质量控制",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 质量控制 > 无损检测
            var wsFolder = GetChildFolder("工艺流程知识库", "质量控制", "无损检测");
            items.Add(new KnowledgeItem
            {
                FolderId = wsFolder.Id,
                Title = "超声波探伤（UT）操作规程",
                Content = @"**执行标准**：NB/T 47013.3-2024

**操作步骤**：
1. 表面处理：清除焊缝两侧各 150mm 范围内的飞溅、氧化皮
2. 耦合剂涂抹：工业浆糊或机油，均匀涂覆
3. 仪器校准：使用 CSK-ⅠA 试块校准声速和探头零点
4. DAC 曲线制作：使用对比试块制作距离-波幅曲线
5. 扫查检测：锯齿形扫查，速度 ≤ 150mm/s
6. 缺陷记录：波幅超过定量线的缺陷记录位置、深度、长度

**评定标准**：
| 缺陷等级 | 判定条件 |
|----------|----------|
| Ⅰ级 | 无任何超标缺陷显示 |
| Ⅱ级 | 单个缺陷指示长度 ≤ T/3（T 为板厚） |
| Ⅲ级 | 单个缺陷指示长度 ≤ 2T/3 |
| Ⅳ级 | 超过 Ⅲ级要求",
                Tags = "无损检测,超声波探伤,UT,操作规程,缺陷评定",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 质量控制 > 尺寸检测
            var ccFolder = GetChildFolder("工艺流程知识库", "质量控制", "尺寸检测");
            items.Add(new KnowledgeItem
            {
                FolderId = ccFolder.Id,
                Title = "三坐标测量仪使用规范",
                Content = @"**开机与校准**：
1. 开机预热 ≥ 30 分钟
2. 使用标准球校准测头（红宝石测头 φ3mm）
3. 校准精度要求：球度误差 ≤ 2μm

**测量要点**：
- 平面度：至少采 9 个点（3×3 网格）
- 圆度：至少采 8 个点（均布）
- 同轴度：基准孔和被测孔各采至少 8 个点
- 位置度：参考基准建立坐标系后测量

**环境要求**：
- 温度：20°C ± 2°C
- 湿度：40%-60%
- 振动：地面振幅 ≤ 0.5μm

**数据处理**：使用 PC-DMIS 软件，报告模板统一采用 CJP-QC-03 格式",
                Tags = "尺寸检测,三坐标测量,操作规范,精度校准",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 安全规程
            var aqFolder = GetFolder("工艺流程知识库", "安全规程");
            items.Add(new KnowledgeItem
            {
                FolderId = aqFolder.Id,
                Title = "高温作业安全防护规程",
                Content = @"**适用范围**：涉及热处理、铸造、锻造、热轧等高温工序的作业人员。

**个人防护装备（PPE）**：
1. 耐高温防护服：耐温 ≥ 300°C，覆盖全身无裸露皮肤
2. 防护面罩：红外反射镀层，透光率 ≤ 0.1%
3. 隔热手套：凯夫拉 + 铝箔复合材料，长度 ≥ 350mm
4. 安全鞋：钢头 + 隔热底，耐温 ≥ 200°C

**作业限制**：
| 环境温度 | 连续作业时间 | 休息时间 |
|----------|-------------|----------|
| < 35°C | 正常作业 | - |
| 35-40°C | ≤ 2h | ≥ 15min/h |
| 40-45°C | ≤ 1h | ≥ 20min/h |
| > 45°C | 停止室外作业 | - |

**应急处置**：发现中暑症状（头晕、恶心、体温升高），立即脱离高温环境，补充含盐饮品。",
                Tags = "安全规程,高温作业,防护装备,应急处置",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // ========================================
            // 设备维护知识库 - 条目
            // ========================================
            order = 0;

            // 预防性维护 > 日常点检
            var djFolder = GetChildFolder("设备维护知识库", "预防性维护", "日常点检");
            items.Add(new KnowledgeItem
            {
                FolderId = djFolder.Id,
                Title = "每日设备点检清单（通用模板）",
                Content = @"**点检时间**：每日开工前 30 分钟

**检查项目**：

| 序号 | 检查项 | 方法 | 判定标准 |
|------|--------|------|----------|
| 1 | 润滑油位 | 目视 | 油标 1/2-2/3 之间 |
| 2 | 异响 | 听觉 | 无异常摩擦、撞击声 |
| 3 | 温升 | 测温枪 | 轴承温升 ≤ 40°C |
| 4 | 紧固件 | 扳手 | 无松动 |
| 5 | 安全防护 | 目视 | 护罩完好、急停有效 |
| 6 | 仪表读数 | 目视 | 压力/流量在正常范围 |
| 7 | 泄漏 | 目视 | 无油、水、气泄漏 |
| 8 | 清洁 | 目视 | 设备表面无积尘油污 |

**异常处理流程**：发现异常 → 立即停机 → 挂""禁止合闸""牌 → 填写点检异常报告 → 通知维修班组",
                Tags = "日常点检,设备维护,检查清单,异常处理",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 预防性维护 > 定期保养
            var byFolder = GetChildFolder("设备维护知识库", "预防性维护", "定期保养");
            items.Add(new KnowledgeItem
            {
                FolderId = byFolder.Id,
                Title = "设备季度保养工作要点",
                Content = @"**保养频率**：每季度第一个工作周执行

**保养内容**：

**机械部分**：
1. 更换或清洗润滑油滤清器
2. 检查皮带张紧度，调整或更换（张紧力按设备说明书）
3. 检查联轴器弹性体磨损情况，磨损 > 2mm 更换
4. 紧固所有地脚螺栓和连接螺栓

**电气部分**：
1. 清理电控柜内积尘，使用压缩空气和防静电刷
2. 检查接触器触点烧蚀情况，接触电阻 > 100μΩ 更换
3. 校验过载保护器 1.05In 不动作、1.2In 2h 内动作
4. 紧固所有接线端子（扭矩按规格）

**记录要求**：填写《设备季度保养记录表》并存档 2 年",
                Tags = "定期保养,季度保养,机械维护,电气维护",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 预防性维护 > 润滑管理
            var rhFolder = GetChildFolder("设备维护知识库", "预防性维护", "润滑管理");
            items.Add(new KnowledgeItem
            {
                FolderId = rhFolder.Id,
                Title = "工业润滑油品选用规范",
                Content = @"**选用原则**：根据工况（速度、载荷、温度）选择合适粘度和类型的润滑油/脂。

**常用油品对照**：

| 设备类型 | 推荐油品 | 粘度等级 | 换油周期 |
|----------|----------|----------|----------|
| 齿轮箱 | 工业闭式齿轮油 L-CKC | ISO VG 220 | 6个月 |
| 液压系统 | 抗磨液压油 L-HM | ISO VG 46 | 12个月 |
| 空压机 | 螺杆空压机油 | ISO VG 46 | 2000h |
| 普通轴承 | 锂基润滑脂 NLGI 2# | - | 3个月 |
| 高温轴承 | 复合磺酸钙脂 | - | 1个月 |

**润滑油劣化指标**：
- 粘度变化 > ±15% 更换
- 酸值增加 > 0.3mgKOH/g 更换
- 含水量 > 0.1% 脱水或更换
- 机械杂质 > 0.05% 过滤或更换",
                Tags = "润滑管理,润滑油,粘度,换油周期,劣化指标",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 故障诊断 > 机械故障 > 轴承故障
            var zcFolder = GetChildFolder("设备维护知识库", "机械故障", "轴承故障");
            items.Add(new KnowledgeItem
            {
                FolderId = zcFolder.Id,
                Title = "滚动轴承常见故障分析与对策",
                Content = @"**故障类型及诊断**：

**1. 疲劳剥落**
现象：轴承运转时有周期性""哒哒""声，振动频谱出现 BPFO/BPFI 特征频率。
原因：疲劳寿命到期、润滑不良、安装不当。
对策：更换轴承，检查润滑和安装精度。

**2. 磨损**
现象：游隙增大，径向跳动超差（> 0.05mm），润滑油中金属粉末含量高。
原因：润滑脂污染、密封失效导致异物进入。
对策：更换轴承和密封件，清洁润滑系统。

**3. 塑性变形**
现象：振动突然增大，保持架断裂可能。
原因：冲击过载、安装敲击不当。
对策：严禁敲击轴承安装，使用液压或热装法。

**4. 电蚀**
现象：滚道表面出现搓板状纹路。
原因：轴电流通过轴承（变频器驱动常见）。
对策：电机增加接地碳刷，轴承选用绝缘型。",
                Tags = "轴承故障,疲劳剥落,磨损,电蚀,故障诊断",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });
            items.Add(new KnowledgeItem
            {
                FolderId = zcFolder.Id,
                Title = "轴承温升异常排查指南",
                Content = @"**正常温升范围**：环境温度 + 15~30°C，最高不超过 80°C。

**排查流程**：
1. 温度测量 → 确认温升速率和最高温度
2. 润滑检查 → 油位是否正常？油脂是否变质？
3. 振动测试 → 是否存在异常频谱？
4. 载荷确认 → 是否超载运行？
5. 对中检查 → 联轴器同轴度 ≤ 0.05mm？
6. 轴承拆检 → 滚道滚动体状态

**异常温度对应可能故障**：

| 温升幅度 | 可能原因 | 优先级 |
|----------|----------|--------|
| 20-30°C | 润滑不足 | 高 |
| 30-50°C | 安装过紧/预紧力过大 | 高 |
| > 50°C | 滚动体/滚道损伤 | 紧急停机 |
| 快速升温 | 保持架断裂 | 立即停机 |",
                Tags = "轴承故障,温升异常,排查指南,润滑,振动",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 故障诊断 > 电气故障
            var elFolder = GetChildFolder("设备维护知识库", "故障诊断", "电气故障");
            items.Add(new KnowledgeItem
            {
                FolderId = elFolder.Id,
                Title = "变频器常见故障代码及排查",
                Content = @"**通用变频器故障代码（西门子/ABB/三菱通用对照）**：

| 故障码 | 含义 | 常见原因 | 排查步骤 |
|--------|------|----------|----------|
| OC/F001 | 过流 | 电机短路、加减速时间过短 | 测电机绝缘，延长加减速时间 |
| OV/F002 | 过压 | 减速时间过短、制动电阻开路 | 延长减速时间，检查制动单元 |
| UV/F003 | 欠压 | 电网电压低、整流桥损坏 | 测输入电压，检查直流母线 |
| OH/F004 | 过热 | 风扇停转、散热片堵塞 | 清洁散热风道，更换风扇 |
| OL/F005 | 过载 | 机械卡死、负载过大 | 检查机械传动，降低负载 |

**维护周期**：
- 散热风扇每 2 年更换
- 直流母线电容每 5 年更换
- 控制板电池每 3 年更换",
                Tags = "电气故障,变频器,故障代码,维护周期",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 故障诊断 > 液压故障
            var yyFolder = GetChildFolder("设备维护知识库", "故障诊断", "液压故障");
            items.Add(new KnowledgeItem
            {
                FolderId = yyFolder.Id,
                Title = "液压系统泄漏诊断与治理",
                Content = @"**泄漏分类**：

**外泄漏**（可见）：
- 管接头泄漏 → 检查密封圈老化/密封面损伤，更换密封件
- 阀组结合面泄漏 → 检查螺栓预紧力和 O 型圈
- 油缸活塞杆泄漏 → 检查防尘圈和主密封磨损

**内泄漏**（不可见）：
- 换向阀阀芯内漏 → 检查阀芯配合间隙（＞5μm 更换）
- 油缸内漏 → 油缸加压后保压测试，10min 压降 > 5% 判内漏
- 液压泵内漏 → 容积效率 < 85% 时需检修

**治理措施优先级**：
1. 紧急处理：管接头复紧（泄漏量 > 1L/h）
2. 周内处理：密封件更换（泄漏量 0.1-1L/h）
3. 计划维修：阀组/泵检修（泄漏量 < 0.1L/h）

**预防措施**：每月巡检 → 紧固管接头 → 检查软管老化 → 更换定期到期的密封件",
                Tags = "液压故障,泄漏,密封件,治理,预防措施",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            // 维修手册
            var wxFolder = GetFolder("设备维护知识库", "维修手册");
            items.Add(new KnowledgeItem
            {
                FolderId = wxFolder.Id,
                Title = "离心泵维修标准作业流程",
                Content = @"**维修触发条件**：
- 流量下降 > 15%（同工况对比）
- 振动值 > ISO 10816-7 标准限值
- 轴承温度 > 85°C
- 运行小时达到大修周期（8000h）

**解体流程**：
1. 断电挂牌 → 关闭进出口阀门 → 排空泵体内介质
2. 拆联轴器护罩 → 脱开联轴器
3. 拆泵盖螺栓 → 吊出泵盖
4. 拆叶轮锁母 → 拉出叶轮
5. 拆轴封（机械密封动静环）
6. 拆轴承端盖 → 拉出转子组件

**检查与更换标准**：
| 部件 | 检查项 | 更换标准 |
|------|--------|----------|
| 叶轮 | 口环间隙 | > 0.5mm（单边） |
| 轴 | 跳动量 | > 0.03mm |
| 轴承 | 游隙 | > 0.05mm（径向） |
| 机械密封 | 动环磨损 | > 0.5mm |

**回装要点**：轴径向跳动 ≤ 0.03mm，联轴器同轴度 ≤ 0.05mm。",
                Tags = "维修手册,离心泵,标准作业,解体流程,更换标准",
                Order = ++order,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "System"
            });

            dbContext.Set<KnowledgeItem>().AddRange(items);
            await dbContext.SaveChangesAsync(cancellationToken);
            Log.Information("[SeedData] 知识条目创建完成，共 {Count} 条", items.Count);
        }

        #endregion
    }
}
