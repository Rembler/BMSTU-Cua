using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cua.Hubs;
using Cua.Models;
using Cua.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NCrontab;

public class MyTestHostedService : BackgroundService
{
    private CrontabSchedule _schedule;
    private DateTime _nextRun;
    private readonly IServiceScopeFactory _scopeFactory;
    private  string Schedule => "*/10 * * * * *"; //Runs every 10 seconds

    public MyTestHostedService(IServiceScopeFactory scopeFactory)
    {
        _schedule = CrontabSchedule.Parse(Schedule,new CrontabSchedule.ParseOptions { IncludingSeconds = true });
        _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            var now = DateTime.Now;
            var nextrun = _schedule.GetNextOccurrence(now);
            if (now > _nextRun)
            {
                await ProcessAsync();
                _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
            }
            await Task.Delay(5000, stoppingToken); //5 seconds delay
        }
        while (!stoppingToken.IsCancellationRequested);
    }

    private async Task ProcessAsync()
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var shared = scope.ServiceProvider.GetRequiredService<SharedHelperService>();

            await shared.SendQueueNotificationsAsync();
            await shared.UpdateOldAppointmentsAsync();
            await shared.SendTimetableNotificationsAsync();     
        }
    }
}