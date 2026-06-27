using CJ.Plug.LicenseApi.Contracts;
using CJ.Plug.LicenseModels;
using Microsoft.AspNetCore.Mvc;

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
                    response.Features = current.License.Features;
                    response.ExpiresAt = current.License.ExpiresAt;
                    response.IsExpired = current.License.IsExpired;
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
                    Features = result.License.Features,
                    ExpiresAt = result.License.ExpiresAt,
                    IsExpired = result.License.IsExpired,
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
                if (request.Features == null || request.Features.Count == 0)
                    return Results.BadRequest(new { error = "请至少选择一个功能" });

                var licenseKey = service.GenerateLicenseKey(
                    request.Features, request.ExpiresAt, request.Licensee);

                var validation = LicenseSigner.ValidateLicense(licenseKey);

                return Results.Ok(new GenerateLicenseResponse
                {
                    LicenseKey = licenseKey,
                    License = validation.License!
                });
            });

            return app;
        }
    }
}
