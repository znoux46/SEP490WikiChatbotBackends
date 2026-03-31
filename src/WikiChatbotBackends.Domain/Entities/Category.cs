using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WikiChatbotBackends.Domain.Entities;

[Table("Categories")]
public class Category
{
    [Column("Id")]
    public Guid Id { get; set; }
    [Column("Name")]
    public string Name { get; set; } = string.Empty;
    [Column("Description")]
    public string Description { get; set; } = string.Empty;
    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
