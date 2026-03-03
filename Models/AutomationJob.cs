namespace GsNetRobo.Models;

public class AutomationJob
{
    public int Id { get; set; }
    public OperationType OperationType { get; set; }
    public string ProgramaSaude { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; } = DateTime.Now;
    public JobStatus Status { get; set; } = JobStatus.Pendente;
    public int TotalDocumentos { get; set; }
    public int DocumentosProcessados { get; set; }
    public string? Erro { get; set; }

    public List<JobDetail> Detalhes { get; set; } = new();
}
