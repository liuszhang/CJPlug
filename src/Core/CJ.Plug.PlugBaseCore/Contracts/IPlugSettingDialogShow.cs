
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json.Nodes;

namespace CJ.Plug.PlugBaseCore.Contracts
{
    public interface IPlugSettingDialogShow
    {
        //Task ShowPlugSettingDialog(string PlugTypeName, int PlugId);
        //Task<JsonObject?> ShowPlugSettingDialog(JsonObject? activity,PlugDataZone? plugDataZone);
        Task<JsonObject?> ShowPlugSettingDialog(ShowPlugSettingContext context);
        //Task ShowPlugSettingDialog(JsonObject activity);
    }



    public interface IPlugSettingDialogShow<T> where T : class
    {
        //Task ShowPlugSettingDialog(string PlugTypeName, int PlugId);
        Task<JsonObject?> ShowPlugSettingDialog(JsonObject? activity, string? WorkflowDefinitionId);
        //Task ShowPlugSettingDialog(JsonObject activity);
    }
}
