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
provider.Mappings[".json"] = "application/json; charset=utf-8";
provider.Mappings[".js"] = "application/javascript; charset=utf-8";
provider.Mappings[".css"] = "text/css; charset=utf-8";
provider.Mappings[".svg"] = "image/svg+xml";
provider.Mappings[".html"] = "text/html; charset=utf-8";
provider.Mappings[".dat"] = "application/octet-stream";
provider.Mappings[".blat"] = "application/octet-stream";

// Em produção, usa wwwroot local (copiado pelo build)
// Em desenvolvimento, usa path relativo ao projeto Blazor
var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");

if (app.Environment.IsDevelopment())
{
    // Desenvolvimento: busca wwwroot do projeto MoneyManager.Web
    var blazorWebProjectPath = Path.Combine(app.Environment.ContentRootPath, "..", "MoneyManager.Web");
    var devWwwrootPath = Path.Combine(blazorWebProjectPath, "wwwroot");
    
    if (Directory.Exists(devWwwrootPath))
    {
        wwwrootPath = devWwwrootPath;
        Console.WriteLine($"[DEV] Usando wwwroot do projeto Blazor: {wwwrootPath}");
    }
    else
    {
        Console.WriteLine($"[DEV] Wwwroot do Blazor não encontrado, usando local: {wwwrootPath}");
    }
}
else
{
    // Produção: usa wwwroot local (copiado pelo publish)
    Console.WriteLine($"[PROD] Usando wwwroot local: {wwwrootPath}");
}

// Verificar se o diretório existe
if (!Directory.Exists(wwwrootPath))
{
    Console.WriteLine($"❌ ERRO: Diretório wwwroot não encontrado em: {wwwrootPath}");
    Console.WriteLine($"ContentRootPath: {app.Environment.ContentRootPath}");
}
else
{
    Console.WriteLine($"✅ Diretório wwwroot encontrado: {wwwrootPath}");
    
    // Listar alguns arquivos para debug
    if (Directory.Exists(Path.Combine(wwwrootPath, "_framework")))
    {
        Console.WriteLine("✅ Pasta _framework encontrada");
    }
    if (File.Exists(Path.Combine(wwwrootPath, "index.html")))
    {
        Console.WriteLine("✅ index.html encontrado");
    }
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
        // Não cachear arquivos da framework em desenvolvimento
        if (app.Environment.IsDevelopment() && 
            (ctx.File.Name == "blazor.webassembly.js" || 
             ctx.File.Name.EndsWith(".wasm") ||
             ctx.File.Name.EndsWith(".json")))
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            ctx.Context.Response.Headers.Pragma = "no-cache";
            ctx.Context.Response.Headers.Expires = "0";
        }
    }
});

// Fallback para index.html (SPA)
app.MapFallback(async (HttpContext context) =>
{
    var indexPath = Path.Combine(wwwrootPath, "index.html");
    
    if (!File.Exists(indexPath))
    {
        Console.WriteLine($"❌ index.html não encontrado em: {indexPath}");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync($"index.html not found at: {indexPath}");
        return;
    }
    
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(indexPath);
});

app.Run();
