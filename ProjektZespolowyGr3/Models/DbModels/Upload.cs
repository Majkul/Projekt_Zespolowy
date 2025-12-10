using System.ComponentModel.DataAnnotations;

namespace ProjektZespolowyGr3.Models.DbModels
{
    public class Upload
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string Extension { get; set; }
        public string Url { get; set; }
        public long SizeBytes { get; set; }

        public int UploaderId { get; set; }
        public User Uploader { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
