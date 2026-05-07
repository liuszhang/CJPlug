using CJ.Plug.Models.EventAggregator;
using CJ.Plug.Models.LogModels;
using Serilog;
using System.Net.Http.Json;
using System.Text.Json;

namespace CJ.Plug.PlugDataZoneApiClient
{
    public partial class PDZApiClient : BaseApiClient, IPDZApiClient
    {
        public PDZApiClient(HttpClient dispatcherClient) : base(dispatcherClient)
        {
        }


        public async Task<PlugDataZone?> GetOrCreatePDZFromPlug(Plug.Models.Plug.Plug? Plug, string? UserName, CancellationToken cancellationToken = default)
        {
            //var filter = new PDZFilter()
            //{
            //    PDZId = UserName + Plug.DefinitionId,
            //    Type = PDZTypeEnum.DesignPDZ.ToString(),
            //    //ProcessJobInstanceId = JobCorrelationId,
            //    PlugDefinitionId = Plug.DefinitionId,
            //    UserName = UserName,
            //    WorkPath = Plug.WorkPath,
            //};
            string PDZId = UserName + Plug.DefinitionId + PDZTypeEnum.Desi.ToString();
            var pdz = await GetPDZByPDZIdAsync(PDZId);
            if (pdz != null)
            {
                CLog.Information(pdz.PDZId, pdz.PDZId);
                return pdz;
            }
            //如果没有PDZ，则找一个已有的PDZ进行复制，如果还是没有，则创建一个新的PDZ
            var filter = new PDZFilter()
            {
                PlugDefinitionId = Plug.DefinitionId,
            };
            var sourcePDZ = await GetPDZByFilter(filter, cancellationToken);
            if (sourcePDZ != null)
            {
                // 复制后必须保存到数据库，否则 PlugDataZoneId 为 0 会导致后续操作出错
                var copiedPDZ = sourcePDZ.CopyPDZ(UserName, PDZTypeEnum.DesignPDZ.ToString(), null);
                return await CreateOrUpdatePDZ(copiedPDZ);
            }


            var newPDZ = new PlugDataZone()
            {
                PDZId = PDZId,
                Type = PDZTypeEnum.DesignPDZ.ToString(),
                PlugDefinitionId = Plug.DefinitionId,
                UserName = UserName,
                PDZWorkPath = Plug.WorkPath,
            };

            var response = await httpClient.PostAsJsonAsync("/api/pdz/createPDZ", newPDZ, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PlugDataZone>(cancellationToken: cancellationToken);
        }

        public async Task<PlugDataZone?> GetOrCreatePDZFromPlugDefinitionId(string PlugDefinitionId, string? UserName, CancellationToken cancellationToken = default)
        {
            string PDZId = UserName + PlugDefinitionId + PDZTypeEnum.Desi.ToString();
            var pdz = await GetPDZByPDZIdAsync(PDZId);
            if (pdz != null)
            {
                CLog.Information(pdz.PDZId, pdz.PDZId);
                return pdz;
            }
            //如果没有PDZ，则找一个已有的PDZ进行复制，如果还是没有，则创建一个新的PDZ
            var filter = new PDZFilter()
            {
                PlugDefinitionId = PlugDefinitionId,
            };
            var sourcePDZ = await GetPDZByFilter(filter, cancellationToken);
            if (sourcePDZ != null)
            {
                // 复制后必须保存到数据库，否则 PlugDataZoneId 为 0 会导致后续操作出错
                var copiedPDZ = sourcePDZ.CopyPDZ(UserName, PDZTypeEnum.DesignPDZ.ToString(), null);
                return await CreateOrUpdatePDZ(copiedPDZ);
            }


            var newPDZ = new PlugDataZone()
            {
                PDZId = PDZId,
                Type = PDZTypeEnum.DesignPDZ.ToString(),
                PlugDefinitionId = PlugDefinitionId,
                UserName = UserName,
            };

            var response = await httpClient.PostAsJsonAsync("/api/pdz/createPDZ", newPDZ, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PlugDataZone>(cancellationToken: cancellationToken);
        }


        private async Task<PlugDataZone?> GetOrCreatePDZFromJob(string? JobCorrelationId, CancellationToken cancellationToken = default)
        {
            var filter = new PDZFilter()
            {
                PDZId = JobCorrelationId,
                Type = PDZTypeEnum.JobPDZ.ToString(),
                JobDefinitionId = JobCorrelationId,
                //PlugDefinitionId = Plug.DefinitionId,
                //UserName = UserName,
                //WorkPath = Plug.WorkPath,
            };
            var pdz = await GetPDZByPDZIdAsync(filter.PDZId);
            if (pdz != null)
            {
                return pdz;
            }
            var response = await httpClient.PostAsJsonAsync("/api/pdz/createPDZ", filter, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PlugDataZone>(cancellationToken: cancellationToken);
        }



        private async Task<PlugDataZone?> GetOrCreatePDZ(string? UserName, string? ProcessDefinitionId, string? PDZType, string? IdentificationId, CancellationToken cancellationToken = default)
        {
            var filter = new PDZFilter()
            {
                PDZId = UserName + ProcessDefinitionId + PDZType + IdentificationId,
                Type = PDZType,
                JobDefinitionId = IdentificationId,
                PlugDefinitionId = ProcessDefinitionId,
                UserName = UserName,
            };
            var pdz = await GetPDZByPDZIdAsync(filter.PDZId);
            if (pdz != null)
            {
                return pdz;
            }
            var response = await httpClient.PostAsJsonAsync("/api/pdz/createPDZ", filter, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PlugDataZone>(cancellationToken: cancellationToken);
        }

        public async Task<PlugDataZone?> GetOrCreateDesignPDZ(string? UserName, string? ProcessDefinitionId)
        {
            var pdz = await GetOrCreatePDZ(UserName, ProcessDefinitionId, null, null);
            //if (pdz == null)
            //{
            //    pdz = new PlugDataZone()
            //    {
            //        PDZId = UserName + ProcessDefinitionId,
            //        Type = PDZTypeEnum.DesignPDZ.ToString(),
            //        ProcessDefinitionId = ProcessDefinitionId,
            //        UserName = UserName,
            //        UserId = UserName,
            //    };
            //}
            return pdz;
        }

        public async Task<PlugDataZone?> GetOrCreateJobPDZ(string? JobCorrelationId)
        {
            var pdz = await GetOrCreatePDZ(null, null, PDZTypeEnum.Job1.ToString(), JobCorrelationId);
            //if (pdz == null)
            //{
            //    pdz = new PlugDataZone()
            //    {
            //        PDZId = UserName + ProcessDefinitionId,
            //        Type = PDZTypeEnum.RunPDZ.ToString(),
            //        ProcessDefinitionId = ProcessDefinitionId,
            //        UserName = UserName,
            //        UserId = UserName,
            //    };
            //}
            return pdz;
        }

        public async Task<PlugDataZone?> GetPDZByPDZIdAsync(string? PDZId)
        {
            if (PDZId == null)
            {
                return null;
            }
            try
            {
                var response = await httpClient.GetFromJsonAsync<PlugDataZone?>($"/api/pdz/getByPDZId/{PDZId}");
                return response;
            }
            catch (Exception ex)
            {
                CLog.Error($"GetPDZByIdAsync failed for PDZId: {PDZId}");
                return null;
            }

        }

        public async Task<PlugDataZone?> GetPDZByIdAsync(int? Id)
        {
            if (Id == null)
            {
                return null;
            }
            try
            {
                var response = await httpClient.GetFromJsonAsync<PlugDataZone?>($"/api/pdz/getById/{Id}");
                return response;
            }
            catch (Exception ex)
            {
                CLog.Error($"GetPDZByIdAsync failed for Id: {Id}");
                return null;
            }

        }


        //统一使用该方法进行PDZ的创建和更新操作,直接更新PDZ的方法比较重，要慎用，优先直接更新PDZ中的子数据
        public async Task<PlugDataZone?> CreateOrUpdatePDZ(PlugDataZone pdz)
        {
            var response = await httpClient.PutAsJsonAsync("/api/PDZ/updatePDZ", pdz);
            if (response.IsSuccessStatusCode)
            {
                StatusReporter.PDZUpdated(pdz.PDZId);
                var result = await response.Content.ReadFromJsonAsync<PlugDataZone>();
                await UpdateDesignPDZ(pdz);
                return result;
            }
            Log.Information($"{pdz.PDZId}:UpdatePDZ failed: {response.StatusCode}", pdz.PDZId);
            return null;
        }
        //根据作业PDZ的来源判断是否需要更新设计PDZ
        private async Task UpdateDesignPDZ(PlugDataZone pdz)
        {
            //如果是设计PDZ，则更新设计PDZ，主要用于更新前端设计中的数据
            if (pdz.Type == PDZTypeEnum.Job1.ToString())
            {
                CLog.Information("同步更新设计PDZ", pdz.PDZId);
                string pdzId = pdz.UserName + pdz.PlugDefinitionId + PDZTypeEnum.Desi.ToString();
                var desiPDZ = await GetPDZByPDZIdAsync(pdzId);
                if (desiPDZ != null)
                {
                    foreach (var v in pdz.PlugVariableDatas)
                    {
                        desiPDZ.SetVariableValue(v.PlugDefinitionId, v.Name, v.Value);
                    }
                    await CreateOrUpdatePDZ(desiPDZ);
                    StatusReporter.PDZUpdated(desiPDZ.PDZId);
                    return;
                }
            }
            return;
        }

        public async Task<bool> DeletePDZ(string? PDZId)
        {
            var response = await httpClient.DeleteAsync($"/api/pdz/deletePDZ/{PDZId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeletePDZByDeletePlug(string? PlugDefinitionId)
        {
            var filter = new PDZFilter()
            {
                PlugDefinitionId = PlugDefinitionId,
            };
            var response = await httpClient.PostAsJsonAsync("/api/pdz/deleteByFilter", filter);
            return response.IsSuccessStatusCode;
        }


        public async Task<PlugDataZone?> CreateJobPDZByCopyPDZ(PlugDataZone? SourcePDZ, string? userName)
        {
            var PDZId = RandomLongIdentityGenerator.GenerateId();
            var newPDZ = new PlugDataZone()
            {
                PDZId = PDZId,
                JobDefinitionId = PDZId,
                PlugDefinitionId = SourcePDZ.PlugDefinitionId,
                Type = PDZTypeEnum.JobPDZ.ToString(),
                UserName = userName
            };
            foreach (var variable in SourcePDZ.PDZVariables)
            {
                variable.Id = null;
                newPDZ.PDZVariables.Add(variable);
            }
            var result = await httpClient.PostAsJsonAsync("/api/pdz/createPDZ", newPDZ);
            if (result.IsSuccessStatusCode)
            {
                return await result.Content.ReadFromJsonAsync<PlugDataZone>();
            }
            CLog.Information($"CreateJobPDZByCopyPDZ failed: {result.StatusCode}");
            return null;
        }


        public async Task<PlugDataZone?> GetPDZByFilter(PDZFilter filter, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/pdz/getPdzByFilter", filter, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PlugDataZone>(cancellationToken: cancellationToken);
            }
            CLog.Information($"GetPDZByFilter failed: {response.StatusCode}");
            return null;
        }
    }
}
