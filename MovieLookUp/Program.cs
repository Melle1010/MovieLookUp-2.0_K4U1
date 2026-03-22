using Microsoft.EntityFrameworkCore;
using MovieLookUp.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("MovieDb"));

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();   // Generates the 'swagger.json' file
    app.UseSwaggerUI(); // Generates the visual webpage you interact with
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
