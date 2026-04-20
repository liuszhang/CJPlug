using CJ.Plug.Models.Relation;
using System.Net.Http.Json;


public class RelationApiClient:BaseApiClient, IRelationApiClient
    {
        public RelationApiClient(HttpClient dispatcherClient):base(dispatcherClient) 
        {
        }

        public async Task<List<CommonRelation?>> GetAllRelations(CancellationToken cancellationToken = default)
        {
            var result = httpClient.GetFromJsonAsAsyncEnumerable<CommonRelation>("/api/relation/getRealations", cancellationToken);
            return await result.ToListAsync(cancellationToken);
        }
        public async Task<CommonRelation?> CreateOrUpdateRelationAsync(CommonRelation request, CancellationToken cancellationToken = default)
        {
            var exist = await GetRealationByFilterAsync(new RelationFilter() { RoleAId = request.RoleAId, RoleBId = request.RoleBId, RelationCategory = request.RelationCategory }, cancellationToken);
            if (exist != null && exist.Count > 0)
            {
                Console.WriteLine("Relation already exist, update it");
                foreach (var r in exist)
                {
                    request.Id = r.Id;
                    await UpdateRelationAsync(request, cancellationToken);
                }
                return exist[0];
            }
            else
            {
                var response = await httpClient.PostAsJsonAsync("/api/relation/createRelation", request, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<CommonRelation>(cancellationToken: cancellationToken);
            }
        }

        private async Task<CommonRelation?> UpdateRelationAsync(CommonRelation request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/relation/updateRelation", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CommonRelation?>(cancellationToken: cancellationToken);
        }

        public async Task<bool> DeleteRealationAsync(CommonRelation request, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/relation/deleteRelation", request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<bool>(cancellationToken: cancellationToken);
        }

        public async Task<List<CommonRelation?>> GetRelationsByCategoryAsync(string Category, CancellationToken cancellationToken = default)
        {
            var result = httpClient.GetFromJsonAsAsyncEnumerable<CommonRelation>($"/api/relation/getRealationsByCategory/{Category}", cancellationToken);
            return await result.ToListAsync(cancellationToken);
        }

        public async Task<List<CommonRelation>?> GetRealationByFilterAsync(RelationFilter filter, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.PostAsJsonAsync("/api/relation/getByFilter", filter, cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CommonRelation>?>(cancellationToken: cancellationToken);
        }

    }

