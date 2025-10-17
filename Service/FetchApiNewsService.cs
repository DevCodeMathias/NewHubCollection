using HubNewsCollection.Domain.Interfaces;
using System.Globalization;

namespace HubNewsCollection.Service
{
    public class FetchApiNewsService : IFetchApiNews
    {
        private readonly HttpClient _httpClient;
        public FetchApiNewsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> FetchNews()
        {

            string url = "https://api.mediastack.com/v1/news?access_key=a5fd2aa1846317bb1ffcbfa646f61384&categories=business";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();



            return content;
        }
    }
}
