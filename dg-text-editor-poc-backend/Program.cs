using dg_text_editor_poc_backend;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddCors();
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IDocumentProvider, DocumentProvider>();
builder.Services.AddScoped<IRevisionLogProvider, RevisionLogProvider>();


var app = builder.Build();

app.UseCors(builder => { builder.WithOrigins("http://localhost:3000").AllowAnyHeader().AllowAnyMethod().AllowCredentials(); });


app.MapHub<EditorHub>("/editor");

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
           Path.Combine(builder.Environment.ContentRootPath, "clientapp/build")),
    RequestPath = "/app"
});

app.Run();
