using CJ.Plug.ApiClient.Contracts;
using Elsa.Api.Client.Extensions;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json.Nodes;

namespace SharedPlugCore
{
    public class PlugSettingDialogCoreShow : IPlugSettingDialogShow
    {
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private MainApiClient MainApiClient { get; set; }= default!;

        public PlugSettingDialogCoreShow(IDialogService dialogService,MainApiClient mainApiClient)
        {
            DialogService = dialogService;
            MainApiClient = mainApiClient;
        }

        public async Task<JsonObject?> ShowPlugSettingDialog(JsonObject? activity, string? WorkflowDefinitionId)
        {
            Console.WriteLine("-----------------begin to set common plug");
                  
            var parameters = new DialogParameters<PlugSettingDialogCore>
            {
                { x => x.Activity, activity},
                { x => x.DefinitionId, WorkflowDefinitionId}
                //{ x => x.ActivityDescriptor, ActivityDescriptor}
                //{ x=>x.Plug, plug}
            };
            //DialogOptions diaOption = new DialogOptions()
            //{
            //    MaxWidth = MaxWidth.Large,
            //    Position = DialogPosition.TopCenter,
            //    BackdropClick = false,
            //    FullWidth = true
            //};
            //Console.WriteLine(plug.PlugTypeKey);
            var dialog = await DialogService.ShowAsync<PlugSettingDialogCore>(activity.GetDisplayText(), parameters);
            var result = await dialog.Result;
            //Console.WriteLine(">>>>>>>>>>dialog result:" + result.Data.ToString());
            var newActivity = result.Data;
            return (JsonObject)newActivity;
        }
    }

}