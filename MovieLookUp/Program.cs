using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using MovieLookUp.Data;
using MovieLookUp.Middleware;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Http.Resilience;
using MovieLookUp.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("MovieClient")
                .AddStandardResilienceHandler();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddCorsPolicy();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("Fixed", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromSeconds(10);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 2;
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("MovieDb"));

#pragma warning disable EXTEXP0018
builder.Services.AddHybridCache();
#pragma warning restore EXTEXP0018

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();

app.Run();
