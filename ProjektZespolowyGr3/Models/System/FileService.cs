using ProjektZespolowyGr3.Models.DbModels;

namespace ProjektZespolowyGr3.Models.System
{
    public interface IFileService
    {
        IEnumerable<(string Field, string Message)> ValidateImages(
            IList<IFormFile>? files,
            int maxCount = 5,
            string field = "PhotoFiles");
        IEnumerable<(string Field, string Message)> ValidateAttachments(
            IList<IFormFile>? files,
            int maxCount = 10,
            string field = "Attachments");
        Task<Upload> SaveFileAsync(IFormFile file, int uploaderId);
        Task<IList<Upload>> SaveFilesAsync(IList<IFormFile> files, int uploaderId);
        void DeleteFile(Upload upload);
        void DeletePhysicalFile(string? url);
    }

    public class FileService : IFileService
    {
        private readonly MyDBContext _context;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxImageSizeBytes = 5L * 1024 * 1024;
        private const long MaxAttachmentSizeBytes = 50L * 1024 * 1024;

        public FileService(MyDBContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public IEnumerable<(string Field, string Message)> ValidateImages(
            IList<IFormFile>? files,
            int maxCount = 5,
            string field = "PhotoFiles")
        {
            if (files == null || files.Count == 0)
                yield break;

            if (files.Count > maxCount)
            {
                yield return (field, $"You can upload a maximum of {maxCount} photos.");
                yield break;
            }

            foreach (var file in files)
            {
                if (file.Length > MaxImageSizeBytes)
                    yield return (field, "Each photo must be less than 5 MB.");

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedImageExtensions.Contains(ext))
                    yield return (field, "Only .jpg, .jpeg, .png files are allowed.");

                if (!file.ContentType.StartsWith("image/"))
                    yield return (field, "Invalid file type.");
            }
        }

        public IEnumerable<(string Field, string Message)> ValidateAttachments(
            IList<IFormFile>? files,
            int maxCount = 10,
            string field = "Attachments")
        {
            if (files == null || files.Count == 0)
                yield break;

            if (files.Count > maxCount)
            {
                yield return (field, $"You can upload a maximum of {maxCount} attachments.");
                yield break;
            }

            foreach (var file in files)
            {
                if (file.Length > MaxAttachmentSizeBytes)
                    yield return (field, "Each attachment must be less than 50 MB.");
            }
        }

        public async Task<Upload> SaveFileAsync(IFormFile file, int uploaderId)
        {
            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var upload = new Upload
            {
                FileName = Path.GetFileName(file.FileName),
                Extension = ext,
                Url = $"/uploads/{fileName}",
                SizeBytes = file.Length,
                UploaderId = uploaderId,
                UploadedAt = DateTime.UtcNow
            };

            _context.Uploads.Add(upload);
            return upload;
        }

        public async Task<IList<Upload>> SaveFilesAsync(
            IList<IFormFile> files,
            int uploaderId)
        {
            var uploads = new List<Upload>(files.Count);
            foreach (var file in files)
                uploads.Add(await SaveFileAsync(file, uploaderId));
            return uploads;
        }

        public void DeleteFile(Upload upload)
        {
            if (upload is null) return;

            DeletePhysicalFile(upload.Url);
            _context.Uploads.Remove(upload);
        }

        public void DeletePhysicalFile(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;

            var path = Path.Combine(_env.WebRootPath, url.TrimStart('/'));
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
