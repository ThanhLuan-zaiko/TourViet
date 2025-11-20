using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TourViet.Models;

[Table("TourImages")]
public class TourImage
{
    [Key]
    public Guid ImageID { get; set; } = Guid.NewGuid();

    [Required]
    public Guid TourID { get; set; }

    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = "AzureBlob";

    [Required]
    public string Url { get; set; } = string.Empty;

    public string? Path { get; set; }

    [MaxLength(255)]
    public string? FileName { get; set; }

    [MaxLength(100)]
    public string? MimeType { get; set; }

    public int? FileSize { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    [Required]
    public bool IsPrimary { get; set; } = false;

    [Required]
    public int SortOrder { get; set; } = 0;

    [MaxLength(500)]
    public string? AltText { get; set; }

    [MaxLength(128)]
    public string? Checksum { get; set; }

    public Guid? UploadedBy { get; set; }

    [Required]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public bool IsPublic { get; set; } = true;

    // Navigation properties
    [ForeignKey("TourID")]
    public virtual Tour Tour { get; set; } = null!;

    [ForeignKey("UploadedBy")]
    public virtual User? UploadedByUser { get; set; }
}