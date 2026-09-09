using APP.Models;

namespace APP.Services.Sharing
{
    public interface IShareCardService
    {
        Task<string> GenerateShareCardAsync(ActivityListItem item);
    }
}
