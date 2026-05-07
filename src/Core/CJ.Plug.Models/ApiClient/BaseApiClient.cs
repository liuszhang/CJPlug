using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public abstract class BaseApiClient
    {
        public HttpClient httpClient = new();
        public readonly HttpClient DispatcherClient;


        public BaseApiClient(HttpClient dispatcherClient)
        {
            DispatcherClient = dispatcherClient;
            //httpClient.BaseAddress = new Uri(DispatcherClient.GetStringAsync("api/dispatch/GetApiServer").Result);
            httpClient.BaseAddress = new Uri(GlobalData.MainApiServer);
            //Console.WriteLine("[BaseApiClient]api server to use is:" + httpClient.BaseAddress?.ToString());
        }
    }

