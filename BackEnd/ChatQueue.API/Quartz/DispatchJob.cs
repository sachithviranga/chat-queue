using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Application.Interfaces.Services;
using Quartz;

namespace ChatQueue.API.Quartz
{
    [DisallowConcurrentExecution] 
    public sealed class DispatchJob : IJob
    {
        private readonly IChatDispatcher _dispatcher;

        public DispatchJob(IChatDispatcher dispatcher) => (_dispatcher) = (dispatcher);

        public async Task Execute(IJobExecutionContext context)
        {
            await _dispatcher.DispatchNextAsync(context.CancellationToken);
        }
    }
}
