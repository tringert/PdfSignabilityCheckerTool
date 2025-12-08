using iText.Kernel.Pdf;

namespace PdfSignabilityCheckerTool;

internal class PdfReaderWrapper
{
    private readonly MemoryStream _ms;

    internal bool IsOpenedWithFullPermission { get; init; }

    internal int Permissions { get; init; }

    internal bool IsEncrypted { get; init; }

    internal PdfDictionary? Trailer { get; init; }

    internal PdfDictionary? PermsDictionary { get; set; }

    internal PdfDictionary? DocMdpDictionary { get; set; }

    internal PdfArray? ReferenceArray { get; set; }

    internal PdfDictionary? ReferenceDictionary { get; set; }

    internal PdfDictionary? TransformParamsDictionary { get; set; }

    internal PdfNumber? P { get; set; }

    public PdfReaderWrapper(MemoryStream ms)
    {
        _ms = ms;
        using PdfReader reader = new(ms);
        using PdfDocument pdf = new(reader);

        IsOpenedWithFullPermission = reader.IsOpenedWithFullPermission();
        Permissions = reader.GetPermissions();
        IsEncrypted = reader.IsEncrypted();
        Trailer = pdf.GetTrailer();

        PdfDictionary? rootCatalog = Trailer.GetAsDictionary(PdfName.Root);

        PermsDictionary = rootCatalog?.GetAsDictionary(PdfName.Perms);
        DocMdpDictionary = PermsDictionary?.GetAsDictionary(PdfName.DocMDP);
        ReferenceArray = DocMdpDictionary?.GetAsArray(PdfName.Reference);
        ReferenceDictionary = ReferenceArray?.GetAsDictionary(0);
        TransformParamsDictionary = ReferenceDictionary?.GetAsDictionary(PdfName.TransformParams);
        P = TransformParamsDictionary?.GetAsNumber(PdfName.P);
    }

    internal PdfReader GetPdfReader()
    {
        _ms.Position = 0;
        return new PdfReader(_ms);
    }
}
