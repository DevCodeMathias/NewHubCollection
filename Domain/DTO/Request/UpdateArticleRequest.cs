using System.ComponentModel.DataAnnotations;

namespace HubNewsCollection.Domain.DTO.Request
{
    public class UpdateArticleRequest
    {
        public string? Title { get; set; }

        public string? Url { get; set; }
    }
}
