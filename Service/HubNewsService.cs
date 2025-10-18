using HubNewsCollection.Domain.DTO.Request;
using HubNewsCollection.Domain.Interfaces;
using HubNewsCollection.Domain.Response;
using System.Collections.Concurrent;
using System.Text.Json;

namespace HubNewsCollection.Service
{
    public class HubNewsService : IHubNewsService
    {
        private readonly IFetchApiNews _fetchApiNews;

        private static readonly ConcurrentDictionary<Guid, Articles> _store = new();

        private static readonly ConcurrentDictionary<string, Guid> _urlIndex =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly object _batchLock = new();

        public HubNewsService(IFetchApiNews fetchApiNews)
        {
            _fetchApiNews = fetchApiNews;
        }

        public Task<List<Articles>> GetFeed()
        {
            try
            {
                var list = _store.Values
                    .OrderByDescending(a => a.published_at)
                    .ToList();

                return Task.FromResult(list);
            }
            catch
            {
                Console.WriteLine("❌ Ocorreu um erro durante a operação.");
                return Task.FromResult(new List<Articles>());
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

                var toInsert = new List<Articles>();

                foreach (var a in payload.data.Where(x => !string.IsNullOrWhiteSpace(x.url)))
                {
                    DateTime? published = a.published_at switch
                    {
                        null => null,
                        DateTime dt when dt.Kind == DateTimeKind.Utc => dt,
                        DateTime dt when dt.Kind == DateTimeKind.Local => dt.ToUniversalTime(),
                        DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                    };

                    if (_urlIndex.ContainsKey(a.url!)) continue;

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
                        url = a.url!
                    };

                    toInsert.Add(article);
                }

                lock (_batchLock)
                {
                    foreach (var art in toInsert)
                    {
                        if (_urlIndex.TryAdd(art.url!, art.id))
                        {
                            _store.TryAdd(art.id, art);
                        }
                    }
                }

                Console.WriteLine($"✅ {toInsert.Count} novas notícias salvas em memória.");
            }
            catch
            {
                Console.WriteLine("❌ Ocorreu um erro durante a operação.");
            }
        }

        public Task<bool> DeleteArticleAsync(Guid id)
        {
            try
            {
                if (_store.TryRemove(id, out var removed))
                {
                    if (!string.IsNullOrWhiteSpace(removed.url))
                    {
                        _urlIndex.TryRemove(removed.url!, out _);
                    }
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }
            catch
            {
                Console.WriteLine("❌ Ocorreu um erro durante a operação.");
                return Task.FromResult(false);
            }
        }

        public Task<Articles?> UpdateArticleAsync(Guid id, UpdateArticleRequest request)
        {
            try
            {
                if (!_store.TryGetValue(id, out var article))
                    return Task.FromResult<Articles?>(null);

                if (request.Title is not null)
                {
                    article.title = request.Title;
                }

                if (!string.IsNullOrWhiteSpace(request.Url) &&
                    !string.Equals(request.Url, article.url, StringComparison.OrdinalIgnoreCase))
                {
                    var newUrl = request.Url!;

                    if (_urlIndex.ContainsKey(newUrl))
                    {
                        return Task.FromResult<Articles?>(null);
                    }

                    lock (_batchLock)
                    {
                        if (!string.IsNullOrWhiteSpace(article.url))
                            _urlIndex.TryRemove(article.url!, out _);

                        article.url = newUrl;
                        _urlIndex[newUrl] = article.id;
                    }
                }

                _store[id] = article;
                return Task.FromResult<Articles?>(article);
            }
            catch
            {
                Console.WriteLine("❌ Ocorreu um erro durante a operação.");
                return Task.FromResult<Articles?>(null);
            }
        }
    }
}
