using AdminApi.DTOs.AuditLog;
using AdminApi.Interfaces;
using Serilog;
using Serilog.Events;
using Shared.Models;

namespace AdminApi.Services
{
    public class AuditLogService : IAuditLogServices
    {
        public async Task LogActionAsync(List<AuditLogDto> logs)
        {
            foreach (var log in logs)
            {
                var logEntry = new AuditLog
                {
                    ActorId = log.UserId,
                    ActorName = log.UserName,
                    Action = log.ActionName,
                    AffectedTable = log.AffectedTable,
                    TimeStamp = DateTime.Now,
                    ObjId = log.ObjId,
                    Exception = log.Exception
                };
                var logger = Log.ForContext("AuditLog", true);

                switch (log.Level)
                {
                    case LogEventLevel.Information:
                        logger.Information("Audit log: {@LogEntry}", logEntry);
                        break;
                    case LogEventLevel.Warning:
                        logger.Warning("Audit log: {@LogEntry}", logEntry);
                        break;
                    case LogEventLevel.Error:
                        logger.Error("Audit log: {@LogEntry}", logEntry);
                        break;
                    default:
                        logger.Debug("Audit log: {@LogEntry}", logEntry);
                        break;
                }
            }
            await Task.CompletedTask;
        }

        public AuditLogDto CreateLog(User user, string action, string affectedTable, string? objId, string? exception, LogEventLevel? level)
        {
            return new AuditLogDto(user.Id, user.FullName, action, affectedTable, objId, exception, level);
        }
    }
}