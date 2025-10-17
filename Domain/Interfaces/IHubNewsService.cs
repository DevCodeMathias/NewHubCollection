using HubNewsCollection.Domain.DTO.Request;
using HubNewsCollection.Domain.Response;

namespace HubNewsCollection.Domain.Interfaces
{
    public interface IHubNewsService
    {
        public Task SyncNews();
        public Task<List<Articles>> GetFeed();
        Task<bool> DeleteArticleAsync(Guid id);
        public Task<Articles?> UpdateArticleAsync(Guid id, UpdateArticleRequest request);

    }
}
