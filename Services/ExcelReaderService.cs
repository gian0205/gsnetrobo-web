using ClosedXML.Excel;

namespace GsNetRobo.Services;

public class ExcelReaderService
{
    public List<string> LerDocumentos(Stream stream)
    {
        var documentos = new List<string>();

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet(1);
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        for (int row = 1; row <= lastRow; row++)
        {
            var valor = worksheet.Cell(row, 1).GetString().Trim();
            if (!string.IsNullOrEmpty(valor))
                documentos.Add(valor);
        }

        return documentos;
    }
}
