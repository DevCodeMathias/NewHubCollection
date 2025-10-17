using HubNewsCollection.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HubNewsCollection.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HubNewsController:ControllerBase
    {
        private readonly IHubNewsService _service;
        public HubNewsController(IHubNewsService service)
        {
            _service = service;
        }

        [HttpGet("notices")]
        public async Task<IActionResult> GetNotices()
        {
            try
            {
                await _service.SyncNews();

                return Created();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erro ao buscar notícias: {ex.Message}");
            }
        }

    }
}
