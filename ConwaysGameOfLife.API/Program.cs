using ConwaysGameOfLife.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Ensure database is created
await app.EnsureDatabaseCreated();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map API endpoints
app.MapBoardEndpoints();
app.MapHealthEndpoints();

app.Run();
