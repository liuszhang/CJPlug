using CJ.Plug.Models.Plug;
using CJ.Plug.Models.PlugAction;
using CJ.Plug.Models.Relation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

public partial class TASApiClient
{
        /// <summary>
        /// 获取动作插头的父插头
        /// </summary>
        /// <param name="parentNode"></param>
        /// <returns></returns>
        public async Task<Plug?> GetParentPlugById(Plug ChildPlug)
        {
            Console.WriteLine("begin to get ParentPlug");
            //string toolAgentTogGet = "";
            //using var Http = new HttpClient();
            //Http.BaseAddress = new Uri(toolAgentTogGet);
            var response = await httpClient.GetAsync("/api/plug/GetParentPlugById/" + ChildPlug.Id);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Plug>();
                return result;
            }
            else
            {
                Console.WriteLine($"Failed to load file tree: {response.ReasonPhrase}");
                return null;
            }
        }

        public async Task<PlugAction?> GetPlugActionById(int? plugActionId)
        {
            if (plugActionId == null)
            {
                return null;
            }
            var response = await httpClient.GetAsync("/api/plug/getPlugActionById/" + plugActionId);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PlugAction>();
                return result;
            }
            else
            {
                Console.WriteLine($"Failed to load file tree: {response.ReasonPhrase}");
                return null;
            }
        }

        public async Task<List<PlugAction>?> GetPlugActionsByPlugIdAsync(int? plugId, CancellationToken cancellationToken = default)
        {
            if (plugId == null)
            {
                return null;
            }
            var response = await httpClient.GetAsync("/api/plug/getPlugActionsByPlugId/" + plugId, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<PlugAction>>(cancellationToken: cancellationToken);
                //Console.WriteLine(JsonSerializer.Serialize(result));
                return result;
            }
            else
            {
                Console.WriteLine($"Failed to load file tree: {response.ReasonPhrase}");
                return null;
            }
        }
        public async Task<List<Plug>?> GetPlugActionsByPlugDefinitionIdAsync(string? plugDefinitionId, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync("/api/plug/getPlugActionsByPlugDefinitionId/" + plugDefinitionId, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<Plug>>(cancellationToken: cancellationToken);
                //Console.WriteLine(JsonSerializer.Serialize(result));
                return result;
            }
            else
            {
                Console.WriteLine($"Failed to get actions: {response.ReasonPhrase}");
                return null;
            }
        }

        public async Task<List<PlugAction>?> GetPlugActionsByPlugId2(int? plugId, CancellationToken cancellationToken = default)
        {
            if (plugId == null)
            {
                return null;
            }
            var response = await httpClient.GetAsync("/api/plug/getPlugActionsByPlugId/" + plugId, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<PlugAction>>(cancellationToken: cancellationToken);
                return result;
            }
            else
            {
                Console.WriteLine($"Failed to load file tree: {response.ReasonPhrase}");
                return null;
            }
        }

        public async Task<PlugAction?> CreatePlugAction(PlugAction newItem, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync<PlugAction>("/api/plug/createPlugAction", newItem, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PlugAction>(cancellationToken: cancellationToken);

                //var relation=new PlugToPlugAction()
                //{
                //    PlugActionId = result.Id,
                //    PlugActionDefinitionId = result.DefinitionId,
                //    PlugId = newItem.ParentPlugId,
                //    PlugDefinitionId = newItem.ParentPlugDefinitionId,
                //    RelationshipType = CJ.Plug.ApiClient.Relationship.RelationshipTypes.新建.ToString()
                //};
                //await httpClient.PostAsJsonAsync<PlugToPlugAction>("/api/plug/createRealation", relation, cancellationToken);
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

        public async Task<PlugAction?> UpdatePlugActionAsync(PlugAction newItem, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PutAsJsonAsync<PlugAction>("/api/plug/updatePlugAction", newItem, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PlugAction>(cancellationToken: cancellationToken);
                return result;
            }
            else
            {
                // 处理错误情况
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error update plugaction: {errorMessage}");
                return null;
            }
        }

        public async Task<bool> DeletePlugAction(PlugAction? item, CancellationToken cancellationToken = default)
        {
            var relationship = new PlugToPlugAction();
            relationship.PlugActionDefinitionId = item?.DefinitionId;
            var result2 = await httpClient.PostAsJsonAsync<PlugToPlugAction>($"/api/plug/deleteRealation", relationship, cancellationToken);
            if (!result2.IsSuccessStatusCode)
            {
                // 处理错误情况
                var errorMessage = await result2.Content.ReadAsStringAsync();
                Console.WriteLine($"Error delete relationship: {errorMessage}");
                return false;
            }
            if (!item.IsRootPlug)
            {
                var itemId = item?.Id;
                var result = await httpClient.DeleteAsync($"/api/plug/deletePlugAction/{itemId}", cancellationToken);
                if (!result.IsSuccessStatusCode)
                {
                    // 处理错误情况
                    var errorMessage = await result.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error delete plugaction: {errorMessage}");
                    return false;
                }
            }
            return true;
        }
        public async Task DeletePlugToPlugActionRealtionship(PlugToPlugAction plugToPlugAction, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync<PlugToPlugAction>("/api/plug/deleteRealation", plugToPlugAction, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // 处理错误情况
                var errorMessage = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error DeletePlugToPlugActionRealtionship: {errorMessage}");
            }
        }

        public async Task SetExecutePlugActionsToPlug(Plug plug, List<PlugAction>? plugActions)
        {
            var relationship = new PlugToPlugAction()
            {
                PlugId = plug.Id,
                PlugDefinitionId = plug.DefinitionId
            };
            if (plugActions == null)
            {
                //Console.WriteLine("plugActions is null");            
                return;
            }
            //清除plug的执行动作
            await DeletePlugToPlugActionRealtionship(relationship);
            foreach (var plugAction in plugActions)
            {
                relationship.PlugActionId = plugAction.Id;
                relationship.PlugActionDefinitionId = plugAction.DefinitionId;
                relationship.RelationshipType = RelationTypes.执行.ToString();
                var result = await httpClient.PostAsJsonAsync<PlugToPlugAction>("/api/plug/createRealation", relationship);
                if (!result.IsSuccessStatusCode)
                {
                    // 处理错误情况
                    var errorMessage = await result.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error SetExecutePlugActionsToPlug: {errorMessage}");
                }
            }
        }

        public async Task<List<PlugAction>?> GetPlugActionsToExecuteByPlugId(int? plugId, CancellationToken cancellationToken = default)
        {
            if (plugId == null)
            {
                return null;
            }
            var response = await httpClient.GetAsync("/api/plug/getRealations", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to GetPlugActionsToExecuteByPlugId: {response.ReasonPhrase}");
                return null;
            }
            var allRelation = await response.Content.ReadFromJsonAsync<List<PlugToPlugAction>>(cancellationToken: cancellationToken);
            if (allRelation == null)
            {
                return null;
            }
            var plugActions = new List<PlugAction>();
            foreach (var relationship in allRelation)
            {
                if (relationship.PlugId == plugId && relationship.RelationshipType == RelationTypes.执行.ToString())
                {
                    var plugAction = await GetPlugActionById(relationship.PlugActionId);
                    if (plugAction != null)
                    {
                        plugActions.Add(plugAction);
                    }
                }

            }
            return plugActions;
        }

        public async Task AddPlugActionToPlug(Plug plug, PlugAction plugAction, RelationTypes relationshipType)
        {
            var relationship = new PlugToPlugAction()
            {
                PlugId = plug.Id,
                PlugDefinitionId = plug.DefinitionId,
                PlugActionDefinitionId = plugAction.DefinitionId,
                PlugActionId = plugAction.Id,
                PlugToolVersion = plug.ToolVersion,
                RelationshipType = relationshipType.ToString()
            };
            var result2 = await httpClient.PostAsJsonAsync<PlugToPlugAction>("/api/plug/createRealation", relationship);
            if (!result2.IsSuccessStatusCode)
            {
                // 处理错误情况
                var errorMessage = await result2.Content.ReadAsStringAsync();
                Console.WriteLine($"Error AddPlugActionToPlug createRealation: {errorMessage}");
            }
        }

        public async Task<PlugAction?> CreateAndAddPlugActionToPlug(Plug plug, PlugAction plugAction, RelationTypes relationshipType)
        {
            plugAction.ParentPlugId = plug.Id;
            plugAction.ParentPlugDefinitionId = plug.DefinitionId;
            plugAction.ParentPlugVersion = plug.ToolVersion;
            plugAction.Id = null;
            plugAction.DefinitionId = RandomLongIdentityGenerator.GenerateId();
            var result = await CreatePlugAction(plugAction);
            if (result == null)
            {
                return null;
            }
            await AddPlugActionToPlug(plug, result, relationshipType);

            return result;
        }


    
}
