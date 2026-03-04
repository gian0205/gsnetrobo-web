using System.Threading.Channels;

namespace GsNetRobo.Services;

public class JobRequest
{
    public int JobId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public List<string> Documentos { get; set; } = new();
    public Models.OperationType Operacao { get; set; }
    public string ProgramaSaude { get; set; } = string.Empty;
    public string Gestor { get; set; } = string.Empty;
}

public class JobQueueService : BackgroundService
{
    private readonly Channel<JobRequest> _channel = Channel.CreateUnbounded<JobRequest>();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobQueueService> _logger;

    public JobQueueService(IServiceScopeFactory scopeFactory, ILogger<JobQueueService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async ValueTask EnfileirarAsync(JobRequest request)
    {
        await _channel.Writer.WriteAsync(request);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobQueueService iniciado.");

        await foreach (var request in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processando job {JobId}", request.JobId);

                using var scope = _scopeFactory.CreateScope();
                var automationService = scope.ServiceProvider.GetRequiredService<GsNetAutomationService>();

                await automationService.ExecutarAsync(
                    request.JobId,
                    request.Usuario,
                    request.Senha,
                    request.Documentos,
                    request.Operacao,
                    request.ProgramaSaude
                );

                _logger.LogInformation("Job {JobId} concluído", request.JobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar job {JobId}", request.JobId);
            }
        }
    }
}
