using Serilog.Events;

namespace AdminApi.DTOs.AuditLog
{
    public class AuditLogDto
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ActionName { get; set; }
        public string AffectedTable { get; set; }
        public string? ObjId { get; set; }
        public string Exception { get; set; }
        public LogEventLevel Level { get; set; }

        public AuditLogDto(string userId, string userName, string actionName, string affectedTable, string? objId, string? exception, LogEventLevel? level)
        {
            UserId = userId;
            UserName = userName;
            AffectedTable = affectedTable;
            ActionName = actionName;
            ObjId = objId;
            Exception = exception ?? "None";
            Level = level ?? LogEventLevel.Information;
        }
    }

}