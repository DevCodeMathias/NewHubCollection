namespace HubNewsCollection.Domain.Interfaces
{
    public interface IFetchApiNews
    {
        public Task<string> FetchNews();
    }
}
