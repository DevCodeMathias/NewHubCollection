using HubNewsCollection.Database;
using HubNewsCollection.Domain.Interfaces;
using HubNewsCollection.Domain.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace HubNewsCollection.Service
{
    public class HubNewsService : IHubNewsService
    {
        private readonly IFetchApiNews _fetchApiNews;
        private readonly AppDbContext _db;


        public HubNewsService(
        IFetchApiNews fetchApiNews,
        AppDbContext db)
        {
            _fetchApiNews = fetchApiNews;
            _db = db;
        }

        public Task<List<Articles>> GetFeed()
        {
            var articles = _db.Articles
                .OrderByDescending(a => a.published_at)
                .ToListAsync();

            return articles;
        }

        public async Task SyncNews()
        {
           
                var json = await _fetchApiNews.FetchNews();

                if (string.IsNullOrWhiteSpace(json))
                {
                    Console.WriteLine("⚠️ A API retornou um corpo vazio.");
                    return;
                }

                var payload = JsonSerializer.Deserialize<ApiFetchResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (payload?.data == null || payload.data.Count == 0)
                {
                    Console.WriteLine("⚠️ Nenhuma notícia foi retornada pela API.");
                    return;
                }

                var fetched = payload.data.Count;

                var existingUrls = await _db.Articles
                    .Select(a => a.url)
                    .ToListAsync();

                var existing = new HashSet<string>(existingUrls, StringComparer.OrdinalIgnoreCase);

                var toInsert = payload.data
                    .Where(a => !string.IsNullOrWhiteSpace(a.url))
                    .Where(a => !existing.Contains(a.url!))
                    .Select(a => new Articles
                    {
                        id = Guid.NewGuid(),
                        title = a.title,
                        author = a.author,
                        source = a.source,
                        category = a.category ?? "business",
                        image = a.image,
                        description = a.description,
                        published_at = a.published_at switch
                        {
                            null => (DateTime?)null,
                            DateTime dt when dt.Kind == DateTimeKind.Utc => dt,
                            DateTime dt when dt.Kind == DateTimeKind.Local => dt.ToUniversalTime(),
                            DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc) 
                        },
                        url = a.url!
                    })
                    .ToList();

                if (toInsert.Count > 0)
                {
                    await _db.Articles.AddRangeAsync(toInsert);
                    await _db.SaveChangesAsync();
                    Console.WriteLine($"✅ {toInsert.Count} novas notícias salvas no banco ({fetched} recebidas da API).");
                }
                else
                {
                    Console.WriteLine("ℹ️ Nenhuma notícia nova para salvar (todas já existem no banco).");
                }

        }


    }
}
