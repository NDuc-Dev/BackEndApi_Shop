namespace AdminApi.Interfaces
{
    public interface IImageServices
    {
        string CreatePathForBase64Img(string pathFor, string imagebase64);
        Task<string> CreatePathForImg(string pathFor, IFormFile image);
        bool ProcessImageExtension(IFormFile image);
    }
}