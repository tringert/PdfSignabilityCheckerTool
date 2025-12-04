using iText.Kernel.Pdf;

namespace PdfSignabilityCheckerTool;

internal class PdfReaderWrapper
{
    private MemoryStream _ms;

    internal bool IsOpenedWithFullPermission { get; init; }

    internal int Permissions { get; init; }

    internal bool IsEncrypted { get; init; }

    internal PdfDictionary Trailer { get; init; }

    internal PdfDictionary RootCatalog => Trailer.GetAsDictionary(PdfName.Root);

    public PdfReaderWrapper(MemoryStream ms)
    {
        _ms = ms;
        using PdfReader reader = new(ms);
        using PdfDocument pdf = new(reader);

        IsOpenedWithFullPermission = reader.IsOpenedWithFullPermission();
        Permissions = reader.GetPermissions();
        IsEncrypted = reader.IsEncrypted();
        Trailer = pdf.GetTrailer();
    }

    internal PdfReader GetPdfReader()
    {
        _ms.Position = 0;
        return new PdfReader(_ms);
    }
}
