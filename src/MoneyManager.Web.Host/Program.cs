using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Configurar tipos MIME para WebAssembly
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".wasm"] = "application/wasm";
provider.Mappings[".json"] = "application/json";
provider.Mappings[".js"] = "application/javascript";
provider.Mappings[".css"] = "text/css";
provider.Mappings[".svg"] = "image/svg+xml";
provider.Mappings[".html"] = "text/html; charset=utf-8";

// Caminho para o wwwroot do Blazor Web
var blazorWebProjectPath = Path.Combine(app.Environment.ContentRootPath, "..", "MoneyManager.Web");
var wwwrootPath = Path.Combine(blazorWebProjectPath, "wwwroot");

// Verificar se o diretório existe
if (!Directory.Exists(wwwrootPath))
{
    Console.WriteLine($"Aviso: Diretório wwwroot não encontrado em: {wwwrootPath}");
    Console.WriteLine($"ContentRootPath: {app.Environment.ContentRootPath}");
}

var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wwwrootPath);

// Servir arquivos estáticos do Blazor WebAssembly
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider,
    ContentTypeProvider = provider,
    OnPrepareResponse = ctx =>
    {
        // Não cachear arquivos da framework
        if (ctx.File.Name == "blazor.webassembly.js" || 
            ctx.File.Name.EndsWith(".wasm") ||
            ctx.File.Name.EndsWith(".json"))
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            ctx.Context.Response.Headers.Pragma = "no-cache";
            ctx.Context.Response.Headers.Expires = "0";
        }
    }
});

// Fallback para index.html (SPA)
var indexPath = Path.Combine(wwwrootPath, "index.html");
app.MapFallback(() => Results.File(indexPath, "text/html; charset=utf-8"));

app.Run();
