using CompraProgramada.Application.Interfaces;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Interfaces;
using CompraProgramada.Infrastructure.Cotacoes;
using CompraProgramada.Infrastructure.Data;
using CompraProgramada.Infrastructure.Kafka;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Port=3307;Database=compra_programada;User=root;Password=SUASENHA;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Registrar IAppDbContext apontando para AppDbContext
builder.Services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

// Services
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<ICestaService, CestaService>();
builder.Services.AddScoped<IMotorCompraService, MotorCompraService>();
builder.Services.AddScoped<IRebalanceamentoService, RebalanceamentoService>();
builder.Services.AddScoped<IContaMasterService, ContaMasterService>();
builder.Services.AddScoped<IPrecoMedioService, PrecoMedioService>();
builder.Services.AddScoped<IIRService, IRService>();

// Infrastructure
builder.Services.AddSingleton<ICotahistParser, CotahistParser>();
builder.Services.AddSingleton<IKafkaProducer, KafkaProducerService>();

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Compra Programada API",
        Version = "v1",
        Description = "Sistema de compra programada de acoes - Desafio Tecnico Itau Corretora"
    });
});

var app = builder.Build();

// Aplicar migrations automaticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

// Swagger sempre ativo para avaliacao
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Compra Programada API v1");
    c.RoutePrefix = string.Empty;
});

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();