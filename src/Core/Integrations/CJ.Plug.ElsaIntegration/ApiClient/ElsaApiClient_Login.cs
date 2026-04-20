using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Requests;
using Elsa.Api.Client.Resources.ActivityDescriptors.Responses;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.ActivityExecutions.Requests;
using Elsa.Api.Client.Resources.Identity.Requests;
using Elsa.Api.Client.Resources.Identity.Responses;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio;
using Elsa.Studio.Contracts;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Services;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Notifications;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Models;
using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Refit;
using Serilog;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ActivityDescriptor = Elsa.Api.Client.Resources.ActivityDescriptors.Models.ActivityDescriptor;

public partial class ElsaApiClient
{

    public async Task<LoginResponse> LoginToElsaAsync([Body] LoginRequest? request=null, CancellationToken cancellationToken = default)
    {
        var url = new Uri($"/elsa/api/identity/login", UriKind.Relative);
        request = request ?? new LoginRequest("admin","password");
        var response = await httpClient.PostAsJsonAsync(url, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        // 反序列化响应体（假设响应是 JSON 格式）
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<LoginResponse>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        Console.WriteLine("[api]login to elsa success");

        return result ?? new LoginResponse(false,"",""); // 处理 null 情况
    }
}

