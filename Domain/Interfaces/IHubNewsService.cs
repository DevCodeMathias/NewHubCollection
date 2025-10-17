using HubNewsCollection.Domain.Response;

namespace HubNewsCollection.Domain.Interfaces
{
    public interface IHubNewsService
    {
        public Task SyncNews();

        public Task<List<Articles>> GetFeed();
     
    }
}
