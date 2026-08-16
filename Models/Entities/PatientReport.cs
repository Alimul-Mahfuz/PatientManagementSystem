namespace PatientManagementSystem.Models.Entities;

public class PatientReport
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
