using CJ.Plug.Models.LogModels;
using CJ.Plug.Models.Plug;
using CJ.Plug.TASApiClient;
using Serilog;
using System.Net.Http.Json;

public partial class TASApiClient:BaseApiClient,ITASApiClient
{

    public TASApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
    {
    }

    //TAS和插头相关
    public async Task<Plug?> CreateNewPlug(Plug newItem, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync<Plug>("/api/plug/createPlug", newItem, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<Plug>(cancellationToken: cancellationToken);
            return result;
        }
        else
        {
            // 处理错误情况
            var errorMessage = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Error creating new plug: {errorMessage}");
            return null;
        }
    }

    public async Task<List<Plug>> GetPlugs(CancellationToken cancellationToken = default)
    {
        //Console.WriteLine("httpclient baseaddress:"+ httpClient.BaseAddress?.ToString());
        var result = httpClient.GetFromJsonAsAsyncEnumerable<Plug>("/api/plug/getPlugs", cancellationToken);
        //var result = await httpClient.PostAsJsonAsync<ToolItem>("/api/tas/createTool", newItem, cancellationToken);
        return await result.ToListAsync();
    }

    public async Task<List<Plug>?> GetCihldPlugsByDefinitionId(string DefinitionId, CancellationToken cancellationToken = default)
    {
        var result = httpClient.GetFromJsonAsAsyncEnumerable<Plug>($"/api/plug/getChildPlugs/{DefinitionId}", cancellationToken);
        return await result.ToListAsync();
    }

    /// <summary>
    /// 通过DefinitionId删除插头
    /// </summary>
    /// <param name="definitionId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> DeletePlugByDefinitionId(string? definitionId, CancellationToken cancellationToken = default)
    {
        var plug = await GetPlugByDefinitionIdAsync(definitionId, cancellationToken);
        if (plug == null)
        {
            Log.Information("Plug not found");
            return false;
        }
        return await DeletePlug(plug.Id);
    }

    public async Task<bool> DeletePlug(int? itemId, CancellationToken cancellationToken = default)
    {
        var plug = await GetPlugById(itemId);
        if (plug is null)
        {
            Log.Information("Plug not found");
            return false;
        }
        //如果是流程插头的删除，需要删除其下所有的插头实例，使用DefinitionID匹配查找
        var childPlugs = await GetCihldPlugsByDefinitionId(plug.DefinitionId);
        if (childPlugs?.Count > 0)
        {
            foreach (var childPlug in childPlugs)
            {
                Log.Information("Deleting child plug: " + childPlug.Id);
                await DeletePlug(childPlug.Id);
            }
        }
        //查找插头的动作，如果不是根插头，也一并删除
        var childPlugActions = await GetPlugActionsByPlugIdAsync(itemId) ?? new();
        foreach (var childPlugAction in childPlugActions)
        {
            if (!childPlugAction.IsRootPlug)
            {
                Log.Information("Deleting child plug: " + childPlugAction.Id);
                await DeletePlug(childPlugAction.Id);
            }
        }

        //最后再删除选定的插头
        var result = await httpClient.DeleteAsync($"/api/plug/deletePlug/{itemId}", cancellationToken);
        if (result.IsSuccessStatusCode)
        {
            return true;
        }
        else
        {
            // 处理错误情况
            var errorMessage = await result.Content.ReadAsStringAsync();
            Console.WriteLine($"Error delete workflow: {errorMessage}");
            return false;
        }
    }



    public async Task<Plug?> UpdatePlugAsync(int? itemId, Plug item, CancellationToken cancellationToken = default)
    {
        //Console.WriteLine(">>>>>>>>>>>>>>from web variables is:"+JsonSerializer.Serialize(workflow.ProcessVariables));
        var result = await httpClient.PutAsJsonAsync($"/api/plug/updatePlug/{itemId}", item, cancellationToken);
        //return true;
        // 检查响应状态码
        if (result.IsSuccessStatusCode)
        {
            var plug = await result.Content.ReadFromJsonAsync<Plug>(cancellationToken: cancellationToken);
            return plug;
        }
        else
        {
            // 处理错误情况
            //var errorMessage = await result.Content.ReadAsStringAsync();
            var errorMessage = await result.Content.ReadAsStringAsync();
            Console.WriteLine($"Error updating tool: {errorMessage}");
            return null;
        }
    }


    public async Task<Plug?> GetPlugById(int? Id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<Plug>($"/api/plug/getById/{Id}", cancellationToken);
        return response;
    }

    public async Task<Plug?> GetPlugByDefinitionIdAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(definitionId))
        {
            CLog.Error("DefinitionId is null or empty");
            Console.WriteLine("DefinitionId is null or empty");
            return null;
        }
        //Console.WriteLine($"/api/plug/getByDefinitionId/{definitionId}");
        var response = await httpClient.GetFromJsonAsync<Plug?>($"/api/plug/getByDefinitionId/{definitionId}", cancellationToken);

        return response;
    }

    public async Task<Plug?> GetRootPlugByTypeNameAsync(string? typeName, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<Plug?>($"/api/plug/getByType/{typeName}", cancellationToken);
            //Log.Information($"准备执行插头：{response.Name}({response.Id})");
            return response;
        }
        catch (Exception ex)
        {
            CLog.Error("Error getting plug by type name");
            Console.WriteLine("Error getting plug by type name");
            return null;
        }
    }



    [Obsolete]
    public async Task<string?> GetPlugVariableValueAsync(int plugId, string variableName)
    {
        var plug = await GetPlugById(plugId);
        if (plug is not null)
        {
            var variable = plug.PlugVariables.Find(v => v.Name == variableName);
            if (variable is not null)
            {
                return variable.Value;
            }
        }
        return null;
    }
    [Obsolete]
    public async Task<string?> GetPlugVariableValueAsync(string plugDefinitionId, string variableName)
    {
        var plug = await GetPlugByDefinitionIdAsync(plugDefinitionId);
        if (plug is not null)
        {
            var variable = plug.PlugVariables.Find(v => v.Name == variableName);
            if (variable is not null)
            {
                return variable.Value;
            }
        }
        return null;
    }
}