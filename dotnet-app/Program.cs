
// ASP.NET Core entry point for the ACORD XML intake app.
// This project exposes the same endpoints through a controller-based setup to match the
// conventional ASP.NET Core folder structure while preserving the XML intake behavior.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();
