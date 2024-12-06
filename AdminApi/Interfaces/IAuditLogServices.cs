using Serilog.Events;
using Shared.Models;

namespace AdminApi.Interfaces
{
    public interface IAuditLogServices
    {
        Task LogActionAsync(User user, string actionName, string? table = null, string? objId = null, string? exception = null, LogEventLevel? level = LogEventLevel.Information);
    }
}