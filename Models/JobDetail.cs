namespace GsNetRobo.Models;

public class JobDetail
{
    public int Id { get; set; }
    public int AutomationJobId { get; set; }
    public string NumeroDocumento { get; set; } = string.Empty;
    public bool Sucesso { get; set; }
    public string? MensagemErro { get; set; }
    public DateTime DataProcessamento { get; set; } = DateTime.Now;

    public AutomationJob AutomationJob { get; set; } = null!;
}
