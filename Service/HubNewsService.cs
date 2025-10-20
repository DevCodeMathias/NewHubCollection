using HubNewsCollection.Domain.DTO.Request;
using HubNewsCollection.Domain.Interfaces;
using HubNewsCollection.Domain.Response;
using HubNewsCollection.Database;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HubNewsCollection.Service
{
    public class HubNewsService : IHubNewsService
    {
        private readonly IFetchApiNews _fetchApiNews;
        private readonly AppDbContext _db;

        public HubNewsService(IFetchApiNews fetchApiNews, AppDbContext db)
        {
            _fetchApiNews = fetchApiNews;
            _db = db;
        }

        public async Task<List<Articles>> GetFeed()
        {
            try
            {
                var list = await _db.Articles
                    .OrderByDescending(a => a.published_at)
                    .ToListAsync();

                return list;
            }
            catch
            {
                Console.WriteLine("❌ Ocorreu um erro durante a operação.");
                return new List<Articles>();
            }
        }

        public async Task SyncNews()
        {
            try
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

                var urlsApi = payload.data
                    .Where(x => !string.IsNullOrWhiteSpace(x.url))
                    .Select(x => x.url!.Trim())
                    .ToList();

                var urlsExistentes = await _db.Articles
                    .Where(a => urlsApi.Contains(a.url!))
                    .Select(a => a.url!)
                    .ToListAsync();

                var novas = new List<Articles>();

                foreach (var a in payload.data.Where(x => !string.IsNullOrWhiteSpace(x.url)))
                {
                    var url = a.url!.Trim();

                    if (urlsExistentes.Contains(url, StringComparer.OrdinalIgnoreCase))
                        continue;

                    DateTime? published = a.published_at switch
                    {
                        null => null,
                        DateTime dt when dt.Kind == DateTimeKind.Utc => dt,
                        DateTime dt when dt.Kind == DateTimeKind.Local => dt.ToUniversalTime(),
                        DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                    };

                    var article = new Articles
                    {
                        id = Guid.NewGuid(),
                        title = a.title,
                        author = a.author,
                        source = a.source,
                        category = a.category ?? "business",
                        image = a.image,
                        description = a.description,
                        published_at = published,
                        url = url
                    };

                    novas.Add(article);
                }

                if (novas.Count == 0)
                {
                    Console.WriteLine("ℹ️ Nenhuma notícia nova para salvar (todas já existem).");
                    return;
                }

                await _db.Articles.AddRangeAsync(novas);
                await _db.SaveChangesAsync();

                Console.WriteLine($"✅ {novas.Count} novas notícias salvas no SQL Server.");
            }
            catch
            {
                Console.WriteLine("❌ Ocorreu um erro durante a operação.");
            }
        }

        public async Task<bool> DeleteArticleAsync(Guid id)
        {
            try
            {
                var entity = await _db.Articles.FindAsync(id);
                if (entity == null) return false;

                _db.Articles.Remove(entity);
                await _db.SaveChangesAsync();
                return true;
            }
            catch
            {
                Console.WriteLine("❌ Ocorreu um erro durante a operação.");
                return false;
            }
        }

        public async Task<Articles?> UpdateArticleAsync(Guid id, UpdateArticleRequest request)
        {
            try
            {
                var article = await _db.Articles.FirstOrDefaultAsync(a => a.id == id);
                if (article == null) return null;

                if (request.Title is not null)
                    article.title = request.Title;

                if (!string.IsNullOrWhiteSpace(request.Url) &&
                    !string.Equals(request.Url, article.url, StringComparison.OrdinalIgnoreCase))
                {
                    var newUrl = request.Url!.Trim();

    
                    var urlEmUso = await _db.Articles
                        .AnyAsync(a => a.id != id && a.url == newUrl);

                    if (urlEmUso)
                        return null;

                    article.url = newUrl;
                }

                _db.Articles.Update(article);
                await _db.SaveChangesAsync();

                return article;
            }
            catch
            {
                Console.WriteLine("❌ Ocorreu um erro durante a operação.");
                return null;
            }
        }
    }
}
