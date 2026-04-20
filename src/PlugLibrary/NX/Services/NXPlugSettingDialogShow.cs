using CJ.Plug_Aspire.Models.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using NX.Pages;
using System.Text.Json.Nodes;
using static MudBlazor.CategoryTypes;

public class NXPlugSettingDialogShow : IPlugSettingDialogShow
{
    [Inject] private IDialogService DialogService { get; set; } = default!;

    public NXPlugSettingDialogShow(IDialogService dialogService)
    {
        DialogService = dialogService;
    }

    public async Task<JsonObject?> ShowPlugSettingDialog(JsonObject? activity, string? WorkflowDefinitionId, ActivityDescriptor? ActivityDescriptor)
    {        
        var typeName = activity?.ToString();
        if (!typeName.Contains("NX"))
        {
            Console.WriteLine("is not nx,return!");
            return null;
        }
        //Console.WriteLine("double click activity id is:" + typeName);
        //var result=await DialogService.ShowMessageBox(typeName,$"{typeName}配置", "保存",null, cancelText: "取消", options);
        Console.WriteLine("is nx,begin setting!");
        DialogOptions options = new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        var parameters = new DialogParameters<NXPlugSettingDialog>
        {            
            { x => x.Activity, activity},
            {x => x.DefinitionId, WorkflowDefinitionId},
            { x => x.ActivityDescriptor, ActivityDescriptor}
        };
        var dialog = await DialogService.ShowAsync<NXPlugSettingDialog>("NX组件", parameters, options);
        var result = await dialog.Result;
        //Console.WriteLine(">>>>>>>>>>dialog result:" + result.Data.ToString());
        var newActivity = result.Data;
        return (JsonObject)newActivity;
    }
}

