using Microsoft.EntityFrameworkCore;
using ServiceMonitoringAPI.Context;
using ServiceMonitoringAPI.Hub;
using ServiceMonitoringAPI.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("policy",
        builder => builder
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
        .SetIsOriginAllowed(_ => true));
});

builder.Services.AddControllers();
builder.Services.AddSingleton<IHubService, HubService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSignalR(e =>
{
    e.EnableDetailedErrors = true;
    e.MaximumReceiveMessageSize = 102400000;
});

builder.WebHost.ConfigureKestrel(options =>
{
    // For dev/test only: HTTP on 44302
    options.ListenAnyIP(44302); // HTTP
    // or for HTTPS:
    // options.ListenAnyIP(44302, o => o.UseHttps());
});

var app = builder.Build();




if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// The pipeline:
app.UseHttpsRedirection();

// **1. UseRouting comes here**
app.UseRouting();

// **2. UseCors after you call UseRouting**
app.UseCors("policy");

// **3. If you need authentication/authorization, put them after routing and before endpoints
// app.UseAuthentication();
app.UseAuthorization();

// **4. Then map your endpoints**
app.MapControllers();
app.MapHub<ServiceHub>("/servicehub");

// Finally, run
app.Run();
