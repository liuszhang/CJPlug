这些是 Blazor 运行时资产（`_framework/*`），**故意随源码入库**，不要删。

## 为什么必须有

`CJ.Plug.HostWebServer/Shell/BlazorFrameworkAssetsEndpoints.cs`（2026-10-30 线上故障修复）把
`/_framework/blazor.web.js` 一类请求挂成端点，**文件源是物理 `wwwroot/_framework/`**
（候选根：`cwd/wwwroot/_framework` → `AppContext.BaseDirectory/wwwroot/_framework`）。

两条供给路径都不覆盖它：
- `MapStaticAssets()` 的清单里没有 `_framework` 条目（实测 `*.staticwebassets.endpoints.json` 命中数为 0，Debug/Release 都一样）；
- .NET SDK 不会为「宿主是 `Sdk.Web` 类库 + 唯一 exe 宿主」这种形态自动把框架 JS 落到 `wwwroot`。

缺了它们的表现是**整站有外观、无事件**（浏览器 `Unexpected token '<'`、`window.Blazor` 恒 undefined）。
2026-10-31《CJPlug 打包按 CJOntology 口径重设计》把载荷改成纯 publish 产出后，
原先藏在共享构建目录里的这几个文件不再进包 ⇒ 由本目录补回（`dotnet publish` 会照原样收 `wwwroot`）。

## 同步口径

- 来源：.NET 10 时代的 Blazor 模板资产（本轮取自旧发布根 `02.Publish/Services/Release/net10.0/wwwroot/_framework`，
  2026-10-30 18:05 写入，现场可用）。
- **升级 .NET 主版本时必须整目录替换**（这些 JS 与运行时主版本绑定）；次要版本升级一般可沿用。
- 只保留 `blazor.web.js` / `blazor.server.js` 及其 `.br` / `.gz`；`.map` 不入库（体积换不来现场收益）。
