using AdminApi.DTOs.AuditLog;
using Serilog.Events;
using Shared.Models;

namespace AdminApi.Interfaces
{
    public interface IAuditLogServices
    {
        Task LogActionAsync(List<AuditLogDto> logs);
        AuditLogDto CreateLog(User user, string actionName, string affectedTable, string? objId = null, string? exception = "None", LogEventLevel? level = LogEventLevel.Information);
    }
}