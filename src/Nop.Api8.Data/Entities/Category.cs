using System.ComponentModel.DataAnnotations.Schema;

namespace Nop.Api8.Data.Entities;

[Table("Category")]
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SeName { get; set; } = string.Empty;
}