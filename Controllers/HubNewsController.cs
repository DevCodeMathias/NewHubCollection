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

        [HttpPost("notices")]
        public async Task<IActionResult> PostNotices()
        {
            await _service.SyncNews();
            return Created();
        }

        [HttpGet("Feed")]
        public async Task<IActionResult> GetFeedNotices()
        {
           var list = await _service.GetFeed();
            return Ok(list);
        }

    }
}
