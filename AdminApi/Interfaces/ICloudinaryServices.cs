namespace AdminApi.Interfaces
{
    public interface ICloudinaryServices
    {
        Task<string> UploadImageAsync(IFormFile file, string folder);
        Task<string> UploadBase64ImageAsync(string base64String, string folder);
    }
}