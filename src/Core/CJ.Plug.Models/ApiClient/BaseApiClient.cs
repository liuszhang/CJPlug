using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public abstract class BaseApiClient
{
    /// <summary>
    /// 业务 API 调用客户端。
    /// <para>⚠ P5.8 C2：必须用 <see cref="InternalApiKey.CreateClient"/> 创建，不能用裸 <c>new HttpClient()</c> ——
    /// 本字段是**绕过 IHttpClientFactory 的裸实例**，拿不到 InternalApiKeyHttpFilter 的统一加工，
    /// 一旦 /api/* 收口，所有业务调用（含 299 处 AuditLogHelper 上报）会立刻 401。</para>
    /// </summary>
    public HttpClient httpClient = InternalApiKey.CreateClient();
    public readonly HttpClient DispatcherClient;


    public BaseApiClient(HttpClient dispatcherClient)
    {
        DispatcherClient = dispatcherClient;
        //httpClient.BaseAddress = new Uri(DispatcherClient.GetStringAsync("api/dispatch/GetApiServer").Result);
        httpClient.BaseAddress = new Uri(GlobalData.MainApiServer);
        //Console.WriteLine("[BaseApiClient]api server to use is:" + httpClient.BaseAddress?.ToString());
    }
}

