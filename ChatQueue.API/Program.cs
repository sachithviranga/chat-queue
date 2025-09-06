using ChatQueue.API.Quartz;
using ChatQueue.Application.Interfaces.Repositories;
using ChatQueue.Application.Interfaces.Services;
using ChatQueue.Application.Services;
using ChatQueue.Domain.Configuration;
using ChatQueue.Domain.Interfaces;
using ChatQueue.Infrastructure.Data.Repositories;
using ChatQueue.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.Configure<QuartzSettings>(builder.Configuration.GetSection("Quartz"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<QuartzSettings>>().Value);

builder.Services.Configure<ChatConfiguration>(builder.Configuration.GetSection("ChatSettings"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<ChatConfiguration>>().Value);


builder.Services.AddSingleton<IChatQueueService, InMemoryChatQueueService>();
builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

builder.Services.AddSingleton<IPollingRepository, InMemoryPollingRepository>();
builder.Services.AddSingleton<ITeamRepository, InMemoryTeamRepository>();
builder.Services.AddSingleton<IAssignmentRepository, InMemoryAssignmentRepository>();
builder.Services.AddSingleton<ISessionQueueRepository, InMemorySessionQueueRepository>();

builder.Services.AddScoped<IChatDispatcher, ChatDispatcher>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IChatMaintenanceService, ChatMaintenanceService>();

builder.Services.Configure<QuartzSettings>(builder.Configuration.GetSection("Quartz"));
var quartzSettings = builder.Configuration.GetSection("Quartz").Get<QuartzSettings>() ?? new QuartzSettings();

builder.Services.AddQuartz(q =>
{
    // ---- InactiveCleanupJob ----
    var cleanupKey = new JobKey("InactiveCleanupJob");
    q.AddJob<InactiveCleanupJob>(opts => opts.WithIdentity(cleanupKey));

    if (!string.IsNullOrWhiteSpace(quartzSettings.InactiveCleanup.Cron))
    {
        q.AddTrigger(t => t
            .ForJob(cleanupKey)
            .WithIdentity("InactiveCleanupTrigger")
            .WithCronSchedule(quartzSettings.InactiveCleanup.Cron));
    }
    else if (quartzSettings.InactiveCleanup.IntervalSeconds is int s && s > 0)
    {
        q.AddTrigger(t => t
            .ForJob(cleanupKey)
            .WithIdentity("InactiveCleanupTrigger")
            .StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInSeconds(s).RepeatForever()));
    }

    // ---- DispatchJob ----
    var dispatchKey = new JobKey("DispatchJob");
    q.AddJob<DispatchJob>(opts => opts.WithIdentity(dispatchKey));

    if (!string.IsNullOrWhiteSpace(quartzSettings.Dispatch.Cron))
    {
        q.AddTrigger(t => t
            .ForJob(dispatchKey)
            .WithIdentity("DispatchTrigger")
            .WithCronSchedule(quartzSettings.Dispatch.Cron));
    }
    else if (quartzSettings.Dispatch.IntervalMilliseconds is int ms && ms > 0)
    {
        q.AddTrigger(t => t
            .ForJob(dispatchKey)
            .WithIdentity("DispatchTrigger")
            .StartNow()
            .WithSimpleSchedule(x => x.WithInterval(TimeSpan.FromMilliseconds(ms)).RepeatForever()));
    }
});


builder.Services.AddQuartzHostedService(opts =>
{
    opts.WaitForJobsToComplete = true;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
