namespace Nop.Api8.Data.Entities;

public class Picture
{
    public int Id { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string SeoFilename { get; set; } = string.Empty;
    public string AltAttribute { get; set; } = string.Empty;
    public string TitleAttribute { get; set; } = string.Empty;
    public bool IsNew { get; set; }
}