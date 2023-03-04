using System.Drawing.Text;
using API.DiscordBot.HostService;
using Auth.Database;
using Crosscutting.TransactionHandling;
using DiscordBot.Application.Implementation;
using DiscordBot.Application.Interface;
using DiscordBot.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;

var builder = WebApplication.CreateBuilder(args);

//ILogger
builder.Logging.ClearProviders().AddConsole();

//Docker
builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
//dotnet tool update --global dotnet-ef
//dotnet ef migrations add 0.4 --project Auth.Database --startup-project BoundCoreWebApplication --context AuthDbContext
//dotnet ef database update --project Auth.Database --startup-project BoundCoreWebApplication --context AuthDbContext
builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseMySql(builder.Configuration.GetConnectionString("BoundcoreMaster") ?? throw new InvalidOperationException(),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("BoundcoreMaster")) ?? throw new InvalidOperationException(),
        x =>
        {

        });
});


//Dependency Injection
builder.Services.Scan(a => a.FromCallingAssembly().AddClasses().AsMatchingInterface().WithScopedLifetime());
//builder.Services.AddScoped<IUnitOfWork<IdentityDb>, UnitOfWork<IdentityDb>>();
builder.Services.AddScoped<IUnitOfWork<AuthDbContext>, UnitOfWork<AuthDbContext>>();
builder.Services.AddScoped<IDiscordBotQueryRepository, DiscordBotQueryRepository>();
builder.Services.AddScoped<IDiscordBotQueryImplementation, DiscordBotQueryImplementation>();
builder.Services.AddScoped<IDiscordBotCommandRepository, DiscordBotCommandRepository>();
builder.Services.AddScoped<IDiscordBotCommandImplementation, DiscordBotCommandImplementation>();
builder.Services.AddScoped<IDiscordGatewayBuyHandlerRepository, DiscordGatewayBuyHandlerRepository>();
builder.Services.AddScoped<IDiscordGatewayBuyHandlerImplementation, DiscordGatewayBuyHandlerImplementation>();
builder.Services.AddScoped<IDiscordBotCleanupRepository, DiscordBotCleanupRepository>();
builder.Services.AddScoped<IDiscordBotCleanupImplementation, DiscordBotCleanupImplementation>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("MyJob");
    q.AddJob<HostService>(opts => 
    {
        opts.WithIdentity(jobKey);
    });

    q.AddTrigger(opts =>
    {
        opts.ForJob(jobKey);
        opts.WithIdentity("MyJob-Trigger");
        opts.WithCronSchedule("0 0 0 ? * * *");
        //opts.WithSimpleSchedule(x=> {
        //    x.WithIntervalInSeconds(1);
        //    x.WithRepeatCount(0);
        //});
    });

    q.UseMicrosoftDependencyInjectionJobFactory();
});

builder.Services.AddQuartzHostedService(opt =>
{
    opt.WaitForJobsToComplete = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();



