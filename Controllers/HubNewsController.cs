using HubNewsCollection.Domain.DTO.Request;
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

        [HttpPost("admin/notices")]
        public async Task<IActionResult> PostNotices()
        {
            await _service.SyncNews();
            return Created("", new { message = "Dados sincronizados com sucesso." });
        }

        [HttpGet("Feed")]
        public async Task<IActionResult> GetFeedNotices()
        {
           var list = await _service.GetFeed();
            return Ok(list);
        }

        [HttpDelete("admin/articles/{id:guid}")]
        public async Task<IActionResult> DeleteArticle([FromRoute] Guid id)
        {
            var deleted = await _service.DeleteArticleAsync(id);

            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }


        [HttpPut("admin/articles/{id:guid}")]
        public async Task<IActionResult> UpdateArticle([FromRoute] Guid id, [FromBody] UpdateArticleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (request.Title is null && request.Url is null)
            {
                return BadRequest(new
                {
                    error = "Informe ao menos um campo para atualizar (title ou url)."
                });
            }

            var updated = await _service.UpdateArticleAsync(id, request);

            if (updated is null)
            {
                return NotFound();
            }

            return Ok(updated);
        }

    }

}

