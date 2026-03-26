using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace IbnElgm3a.Services
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Saves a file to the storage and returns the public URL or relative path.
        /// </summary>
        Task<string> SaveFileAsync(IFormFile file, string folder);

        /// <summary>
        /// Deletes a file from the storage.
        /// </summary>
        Task DeleteFileAsync(string fileUrl, string folder);
    }
}
