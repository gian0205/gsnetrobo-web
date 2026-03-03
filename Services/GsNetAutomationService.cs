using GsNetRobo.Data;
using GsNetRobo.Models;
using Microsoft.EntityFrameworkCore;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace GsNetRobo.Services;

public class GsNetAutomationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GsNetAutomationService> _logger;

    public GsNetAutomationService(IServiceScopeFactory scopeFactory, ILogger<GsNetAutomationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task ExecutarAsync(int jobId, string usuario, string senha, List<string> documentos, OperationType operacao, string programaSaude)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var job = await db.Jobs.FindAsync(jobId);
        if (job == null) return;

        job.Status = JobStatus.Executando;
        await db.SaveChangesAsync();

        IWebDriver? driver = null;

        try
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless=new");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");

            driver = new ChromeDriver(ChromeDriverService.CreateDefaultService(), options, TimeSpan.FromMinutes(5));
            driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);

            // Login no GSNet
            driver.Navigate().GoToUrl("http://gsnet.saude.sp.gov.br");

            var login = driver.FindElement(By.Id("TextBoxUsuario"));
            login.Clear();
            login.SendKeys(usuario);

            var senhaField = driver.FindElement(By.Id("TextBoxSenha"));
            senhaField.Clear();
            senhaField.SendKeys(senha);

            new SelectElement(driver.FindElement(By.Id("ListBoxGestor")))
                .SelectByText("(11) - GAB.DO SECRETÁRIO - GS");

            driver.FindElement(By.Id("ButtonLogin")).Click();

            new SelectElement(driver.FindElement(By.Id("ListBoxLocal")))
                .SelectByText(programaSaude);

            // Navegar ao menu de documentos
            if (operacao == OperationType.Desaprovar)
            {
                driver.FindElement(By.Id("Stm0p0i6eTX")).Click();
                driver.FindElement(By.Id("Stm0p18i1eTX")).Click();
            }
            else
            {
                driver.FindElement(By.Id("Stm0p0i5eTX")).Click();
                driver.FindElement(By.Id("Stm0p16i1eTX")).Click();
            }

            // Processar cada documento
            for (int i = 0; i < documentos.Count; i++)
            {
                var fatura = documentos[i];
                var detalhe = new JobDetail
                {
                    AutomationJobId = jobId,
                    NumeroDocumento = fatura,
                    DataProcessamento = DateTime.Now
                };

                try
                {
                    ExecutarAcao(driver, fatura, operacao);
                    detalhe.Sucesso = true;
                }
                catch (Exception ex)
                {
                    detalhe.Sucesso = false;
                    detalhe.MensagemErro = ex.Message;
                    _logger.LogWarning(ex, "Erro ao processar documento {Fatura}", fatura);
                }

                db.JobDetails.Add(detalhe);
                job.DocumentosProcessados = i + 1;
                await db.SaveChangesAsync();
            }

            job.Status = JobStatus.Concluido;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro fatal no job {JobId}", jobId);
            job.Status = JobStatus.Erro;
            job.Erro = ex.Message;
        }
        finally
        {
            try { driver?.Quit(); } catch { }
            await db.SaveChangesAsync();
        }
    }

    private void ExecutarAcao(IWebDriver driver, string fatura, OperationType operacao)
    {
        switch (operacao)
        {
            case OperationType.Efetivar:
                NavegarParaDocumento(driver, fatura);
                driver.FindElement(By.Id("ButtonItem")).Click();
                var btnEfetivar = driver.FindElement(By.Id("ButtonAprovarEfetivar"));
                btnEfetivar.SendKeys(OpenQA.Selenium.Keys.Enter);
                AceitarAlerta(driver);
                break;

            case OperationType.Reservar:
                NavegarParaDocumento(driver, fatura);
                driver.FindElement(By.Id("ButtonItem")).Click();
                var btnReservar = driver.FindElement(By.Id("ButtonAprovarReserva"));
                btnReservar.SendKeys(OpenQA.Selenium.Keys.Enter);
                AceitarAlerta(driver);
                AceitarAlerta(driver);
                break;

            case OperationType.EfetivarReservados:
                NavegarParaDocumento(driver, fatura);
                var btnEfRes = driver.FindElement(By.Id("ButtonEfetivar"));
                btnEfRes.SendKeys(OpenQA.Selenium.Keys.Enter);
                AceitarAlerta(driver);
                break;

            case OperationType.Deletar:
                NavegarParaDocumento(driver, fatura);
                driver.FindElement(By.Id("ButtonDelete")).Click();
                AceitarAlerta(driver);
                break;

            case OperationType.Desaprovar:
                // Desaprovar usa pesquisa ao invés de link direto
                driver.FindElement(By.Id("Stm0p0i6eTX")).Click();
                driver.FindElement(By.Id("Stm0p18i1eTX")).Click();
                var campoPesquisa = driver.FindElement(By.Id("TextBoxDocumento"));
                campoPesquisa.Click();
                campoPesquisa.Clear();
                campoPesquisa.SendKeys(fatura);
                driver.FindElement(By.Id("ButtonPesquisar")).Click();
                driver.FindElement(By.Id("DataGridPesquisa__ctl3_ImageButtonDetalhe")).Click();
                var btnReprovar = driver.FindElement(By.Id("ButtonReprovar"));
                btnReprovar.SendKeys(OpenQA.Selenium.Keys.Enter);
                AceitarAlerta(driver);
                AceitarAlerta(driver);
                break;
        }
    }

    private void NavegarParaDocumento(IWebDriver driver, string fatura)
    {
        var link = driver.FindElement(By.PartialLinkText(fatura)).GetAttribute("href")
            ?? throw new InvalidOperationException($"Link nao encontrado para documento {fatura}");
        driver.Navigate().GoToUrl(link);
    }

    private void AceitarAlerta(IWebDriver driver)
    {
        try
        {
            driver.SwitchTo().Alert().Accept();
        }
        catch (NoAlertPresentException)
        {
            // Sem alerta para aceitar
        }
    }
}
