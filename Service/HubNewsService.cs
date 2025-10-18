using HubNewsCollection.Domain.DTO.Request;
using HubNewsCollection.Domain.Interfaces;
using HubNewsCollection.Domain.Response;
using System.Collections.Concurrent;
using System.Text.Json;

namespace HubNewsCollection.Service
{
    /// <summary>
    /// Implementação in-memory do HubNewsService (sem banco).
    /// Os dados se perdem quando a aplicação reinicia.
    /// </summary>
    public class HubNewsService : IHubNewsService
    {
        private readonly IFetchApiNews _fetchApiNews;

        // Armazena os artigos por Id
        private static readonly ConcurrentDictionary<Guid, Articles> _store = new();

        // Índice auxiliar para deduplicar por URL (case-insensitive)
        private static readonly ConcurrentDictionary<string, Guid> _urlIndex =
            new(StringComparer.OrdinalIgnoreCase);

        // Lock apenas para operações de “lote” (ex.: SyncNews faz AddRange lógico)
        private static readonly object _batchLock = new();

        public HubNewsService(IFetchApiNews fetchApiNews)
        {
            _fetchApiNews = fetchApiNews;
        }

        public Task<List<Articles>> GetFeed()
        {
            var list = _store.Values
                .OrderByDescending(a => a.published_at)
                .ToList();

            return Task.FromResult(list);
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
            var toInsert = new List<Articles>();

            foreach (var a in payload.data.Where(x => !string.IsNullOrWhiteSpace(x.url)))
            {
                // Normaliza published_at para UTC (ou null)
                DateTime? published = a.published_at switch
                {
                    null => null,
                    DateTime dt when dt.Kind == DateTimeKind.Utc => dt,
                    DateTime dt when dt.Kind == DateTimeKind.Local => dt.ToUniversalTime(),
                    DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                };

                // Se já existe por URL, ignora (dedupe)
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

            if (toInsert.Count == 0)
            {
                Console.WriteLine("ℹ️ Nenhuma notícia nova para salvar (todas já existem).");
                return;
            }

            // Inserção em “lote” com lock curto pra consistência do índice
            lock (_batchLock)
            {
                foreach (var art in toInsert)
                {
                    if (_urlIndex.TryAdd(art.url!, art.id))
                    {
                        _store.TryAdd(art.id, art);
                    }
                    // se falhar o TryAdd no índice, outra thread já inseriu a mesma URL
                }
            }

            Console.WriteLine($"✅ {toInsert.Count} novas notícias salvas em memória ({fetched} recebidas da API).");
        }

        public Task<bool> DeleteArticleAsync(Guid id)
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

        public Task<Articles?> UpdateArticleAsync(Guid id, UpdateArticleRequest request)
        {
            if (!_store.TryGetValue(id, out var article))
                return Task.FromResult<Articles?>(null);

            // Atualizar título
            if (request.Title is not null)
            {
                article.title = request.Title;
            }

            // Atualizar URL com manutenção do índice
            if (!string.IsNullOrWhiteSpace(request.Url) &&
                !string.Equals(request.Url, article.url, StringComparison.OrdinalIgnoreCase))
            {
                var newUrl = request.Url!;

                // Garante unicidade por URL
                if (_urlIndex.ContainsKey(newUrl))
                {
                    // já existe artigo com essa URL: não permite trocar
                    // (se quiser permitir, você pode mover/mesclar, mas aqui bloqueamos)
                    return Task.FromResult<Articles?>(null);
                }

                lock (_batchLock)
                {
                    // remove índice antigo (se houver)
                    if (!string.IsNullOrWhiteSpace(article.url))
                        _urlIndex.TryRemove(article.url!, out _);

                    // aplica alteração e registra novo índice
                    article.url = newUrl;
                    _urlIndex[newUrl] = article.id;
                }
            }

            // Commit no dicionário (idempotente, já aponta pro mesmo objeto)
            _store[id] = article;

            return Task.FromResult<Articles?>(article);
        }
    }
}
