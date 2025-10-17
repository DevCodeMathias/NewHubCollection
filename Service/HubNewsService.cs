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

                var fetched = payload.data.Count;

                var existingUrls = await _db.Articles
                    .Select(a => a.Url)
                    .ToListAsync();

                var existing = new HashSet<string>(existingUrls, StringComparer.OrdinalIgnoreCase);

                var toInsert = payload.data
                    .Where(a => !string.IsNullOrWhiteSpace(a.Url))
                    .Where(a => !existing.Contains(a.Url!))
                    .Select(a => new Articles
                    {
                        Title = a.Title,
                        Author = a.Author,
                        Source = a.Source,
                        Category = a.Category ?? "business",
                        Image = a.Image,
                        Description = a.Description,
                        Published_at = a.Published_at switch
                        {
                            null => (DateTime?)null,
                            DateTime dt when dt.Kind == DateTimeKind.Utc => dt,
                            DateTime dt when dt.Kind == DateTimeKind.Local => dt.ToUniversalTime(),
                            DateTime dt => DateTime.SpecifyKind(dt, DateTimeKind.Utc) // Unspecified -> Utc
                        },
                        Url = a.Url!
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
            catch (JsonException ex)
            {
                Console.WriteLine($"❌ Erro ao desserializar o JSON da API: {ex.Message}");
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"❌ Erro ao salvar no banco de dados: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"❌ Erro de conexão com a API externa: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro inesperado: {ex.Message}");
            }
        }
    }
}
