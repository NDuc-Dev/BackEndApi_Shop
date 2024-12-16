using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
namespace AdminApi.Services
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is null or empty");

            using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder, // Tùy chỉnh thư mục
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Failed to upload image to Cloudinary");

            return uploadResult.SecureUrl.ToString(); // Trả về URL của ảnh đã upload
        }

        public async Task<string> UploadBase64ImageAsync(string base64String, string fileName, string folder)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                throw new ArgumentException("Base64 string is null or empty");

            byte[] imageBytes = Convert.FromBase64String(base64String);

            using var stream = new MemoryStream(imageBytes);

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folder,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception("Failed to upload image to Cloudinary");

            return uploadResult.SecureUrl.ToString();
        }
    }
}