using System.ComponentModel.DataAnnotations;

namespace HubNewsCollection.Domain.Response
{
    public class Articles
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Author { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string? Image { get; set; }
        public string Category { get; set; }
        public string? Source { get; set; }  
        public DateTime? Published_at { get; set; }
    }
}
