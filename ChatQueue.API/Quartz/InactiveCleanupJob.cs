using ChatQueue.Application.Interfaces.Services;
using ChatQueue.Application.Services;
using ChatQueue.Domain.Configuration;
using ChatQueue.Domain.Enums;
using ChatQueue.Domain.Interfaces;
using Quartz;

namespace ChatQueue.API.Quartz
{
    [DisallowConcurrentExecution]
    public sealed class InactiveCleanupJob : IJob
    {
        private readonly IChatMaintenanceService _chatMaintenanceService;
        public InactiveCleanupJob(IChatMaintenanceService chatMaintenanceService) => _chatMaintenanceService = chatMaintenanceService;
        public async Task Execute(IJobExecutionContext context)
        {
            await _chatMaintenanceService.CleanupInactiveAsync(context.CancellationToken);
        }
    }
}
