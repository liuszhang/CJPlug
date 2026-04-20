using CJ.Plug.Models.EventAggregator;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.PlugDataZoneApiClient;
using Elsa.Api.Client.Extensions;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CJ.Plug.PlugBaseCore
{
    public class PlugSettingDialogCoreShow : IPlugSettingDialogShow
    {
        [Inject] private IDialogService DialogService { get; set; } = default!;
        [Inject] private MainApiClient MainApiClient { get; set; }= default!;

        private IPDZApiClient PDZApiClient { get; set; }

        public PlugSettingDialogCoreShow(IDialogService dialogService,MainApiClient mainApiClient,IPDZApiClient pDZApiClient)
        {
            DialogService = dialogService;
            MainApiClient = mainApiClient;
            PDZApiClient = pDZApiClient;
        }

        public async Task<JsonObject?> ShowPlugSettingDialog(ShowPlugSettingContext context)
        {
            Console.WriteLine("-----------------begin to set common plug");
            var activity = context.Activity;
            var PlugDataZone = context.PlugDataZone;
            var SetMenuNotify= context.SetMenuNotify;

            var rootPDZJosn =JsonSerializer.Serialize(PlugDataZone);
            var parameters = new DialogParameters<PlugSettingDialogCore>
            {
                { x => x.Activity, activity},
                { x => x.PlugDataZone, PlugDataZone},
                { x => x.SetMenuNofity, SetMenuNotify}
            };
            //Log.Information("OpenMenu:" + "False");
            var dialog = await DialogService.ShowAsync<PlugSettingDialogCore>(activity.GetDisplayText(), parameters);
            var result = await dialog.Result;
            //Console.WriteLine(">>>>>>>>>>dialog result:" + result.Data.ToString());
            if (result.Canceled)
            {
                //将修改过的PDZ还原回去
                //Log.Information("Dialog canceled");
                var rootPDZ = JsonSerializer.Deserialize<PlugDataZone>(rootPDZJosn);
                await PDZApiClient.CreateOrUpdatePDZ(rootPDZ);
                StatusReporter.PDZUpdated(rootPDZ.PDZId);
                return null;
            }
            else
            {
                //var pdz = await MainApiClient.GetPDZById(PlugDataZone.PDZId);
                //await PDZUpdated.InvokeAsync(pdz);
            }
            var newActivity = result.Data;
            return (JsonObject)newActivity;
        }
    }

}