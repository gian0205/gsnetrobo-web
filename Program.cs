using GsNetRobo.Components;
using GsNetRobo.Data;
using GsNetRobo.Models;
using GsNetRobo.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Persiste chaves de Data Protection no volume montado (evita erro de antiforgery após restart)
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/data/keys"));

// Services
builder.Services.AddScoped<ExcelReaderService>();
builder.Services.AddScoped<GsNetAutomationService>();
builder.Services.AddSingleton<JobQueueService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<JobQueueService>());

// SignalR (erros detalhados para diagnóstico do circuit Blazor)
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
});

// Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Auto-create database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    // Habilita WAL mode para melhor concorrência em ambiente Docker
    try { db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;"); } catch { }
    // Adiciona coluna Gestor para bancos de dados existentes
    try { db.Database.ExecuteSqlRaw("ALTER TABLE Jobs ADD COLUMN Gestor TEXT NOT NULL DEFAULT ''"); } catch { }
    // Cria tabela ProgramasSaude para bancos de dados existentes
    try { db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS ProgramasSaude (Id INTEGER PRIMARY KEY AUTOINCREMENT, Nome TEXT NOT NULL, Ativo INTEGER NOT NULL DEFAULT 1)"); } catch { }
    // Seed programas padrão se tabela estiver vazia
    if (!db.ProgramasSaude.Any())
    {
        var defaults = new[]
        {
            "CEAF - RE", "CEBAF - DOSE CERTA", "CEBAF - SAÚDE DA MULHER",
            "CEBAF - OUTROS", "CESAF ", "CVE", "DEMANDA EXTRAORDINARIA",
            "ONCO MS", "RV - MEDICAMENTOS - HEMO REDE",
            "RV - MEDICAMENTOS - CRATOD", "RV - MEDICAMENTOS - CRT/AIDS", "COVID-19"
        };
        foreach (var nome in defaults)
            db.ProgramasSaude.Add(new ProgramaSaudeItem { Nome = nome });
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
