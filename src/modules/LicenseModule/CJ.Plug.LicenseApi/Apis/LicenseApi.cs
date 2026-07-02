using CJ.Plug.LicenseApi.Contracts;
using CJ.Plug.LicenseModels;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace CJ.Plug.LicenseApi.Apis
{
    /// <summary>
    /// 许可证管理 API 端点
    /// </summary>
    public static class LicenseApi
    {
        public static IEndpointRouteBuilder MapLicenseApi(this IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("api/license").WithTags("许可证管理");

            // GET /api/license/status — 获取当前许可证状态
            api.MapGet("/status", (ILicenseService service) =>
            {
                var current = service.GetCurrentLicense();
                var response = new LicenseStatusResponse
                {
                    IsActivated = current.IsValid,
                    Message = current.IsValid ? "已激活" : current.ErrorMessage
                };

                if (current.IsValid && current.License != null)
                {
                    response.Licensee = current.License.Licensee;
                    response.IssuedAt = current.License.IssuedAt;
                    response.CodeExpiresAt = current.License.CodeExpiresAt;
                    response.IsExpired = current.License.CodeExpiresAt.HasValue
                        && current.License.CodeExpiresAt.Value < DateTime.UtcNow;
                }

                return Results.Ok(response);
            });

            // POST /api/license/activate — 激活许可证
            api.MapPost("/activate", ([FromBody] ActivateLicenseRequest request, ILicenseService service) =>
            {
                if (string.IsNullOrWhiteSpace(request.LicenseKey))
                    return Results.BadRequest(new { error = "许可证密钥不能为空" });

                var result = service.ActivateLicense(request.LicenseKey);
                if (!result.IsValid)
                    return Results.BadRequest(new { error = result.ErrorMessage });

                var response = new LicenseStatusResponse
                {
                    IsActivated = true,
                    Licensee = result.License!.Licensee,
                    IssuedAt = result.License.IssuedAt,
                    CodeExpiresAt = result.License.CodeExpiresAt,
                    IsExpired = result.License.CodeExpiresAt.HasValue
                        && result.License.CodeExpiresAt.Value < DateTime.UtcNow,
                    Message = "激活成功"
                };

                return Results.Ok(response);
            });

            // POST /api/license/revoke — 撤销许可证
            api.MapPost("/revoke", (ILicenseService service) =>
            {
                var success = service.RevokeLicense();
                return success
                    ? Results.Ok(new { message = "许可证已撤销" })
                    : Results.Problem("撤销失败");
            });

            // POST /api/license/generate — 生成许可证（管理后台）
            api.MapPost("/generate", ([FromBody] GenerateLicenseRequest request, ILicenseService service) =>
            {
                if (string.IsNullOrWhiteSpace(request.Licensee))
                    return Results.BadRequest(new { error = "被许可人不能为空" });

                var licenseKey = service.GenerateLicenseKey(request.Licensee, request.ValidDays);

                var validation = LicenseSigner.ValidateLicense(licenseKey);

                return Results.Ok(new GenerateLicenseResponse
                {
                    LicenseKey = licenseKey,
                    License = validation.License!
                });
            });

            // ═══════════════════════════════════════════════════════
            // 升级 / 支付
            // ═══════════════════════════════════════════════════════

            // POST /api/license/upgrade/create — 创建升级订单
            api.MapPost("/upgrade/create", (ILicenseService service) =>
            {
                var response = service.CreateUpgradeOrder();
                return Results.Ok(response);
            });

            // GET /api/license/upgrade/status — 查询订单状态
            api.MapGet("/upgrade/status", (string orderId, ILicenseService service) =>
            {
                if (string.IsNullOrWhiteSpace(orderId))
                    return Results.BadRequest(new { error = "订单号不能为空" });

                var response = service.GetUpgradeOrderStatus(orderId);
                return Results.Ok(response);
            });

            // POST /api/license/upgrade/confirm — 确认支付（模拟回调）
            api.MapPost("/upgrade/confirm", (string orderId, ILicenseService service) =>
            {
                if (string.IsNullOrWhiteSpace(orderId))
                    return Results.BadRequest(new { error = "订单号不能为空" });

                var response = service.ConfirmUpgradePayment(orderId);
                return Results.Ok(response);
            });

            // POST /api/license/upgrade/pay-callback — 码支付异步回调
            api.MapPost("/upgrade/pay-callback", async (HttpRequest request, ILicenseService service) =>
            {
                var form = await request.ReadFormAsync();
                var orderId = form["out_trade_no"].FirstOrDefault();
                var money = form["money"].FirstOrDefault();
                var type = form["type"].FirstOrDefault();
                var sign = form["sign"].FirstOrDefault();
                var tradeNo = form["trade_no"].FirstOrDefault();

                if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(sign))
                {
                    Log.Warning("码支付回调参数不完整：orderId={OrderId}, sign={Sign}", orderId, sign);
                    return Results.Text("fail");
                }

                var result = service.VerifyPaymentCallback(orderId, money ?? "", type ?? "", sign, tradeNo);
                if (result == null)
                {
                    Log.Warning("码支付回调验签失败：orderId={OrderId}", orderId);
                    return Results.Text("fail");
                }

                Log.Information("码支付回调处理成功：orderId={OrderId}, status={Status}", orderId, result.Status);
                return Results.Text("success");
            });

            return app;
        }
    }
}
