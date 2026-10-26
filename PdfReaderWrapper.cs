using iText.Kernel.Pdf;

namespace PdfSignabilityCheckerTool;

internal class PdfReaderWrapper
{
    private readonly byte[] _pdfBytes;
    internal bool IsOpenedWithFullPermission { get; }
    internal int Permissions { get; }
    internal bool IsEncrypted { get; }
    internal int CertificationLevel { get; }

    public PdfReaderWrapper(MemoryStream ms)
    {
        _pdfBytes = ms.ToArray();

        using PdfDocument pdf = OpenDocument();
        PdfReader reader = pdf.GetReader();

        IsOpenedWithFullPermission = reader.IsOpenedWithFullPermission();
        Permissions = reader.GetPermissions();
        IsEncrypted = reader.IsEncrypted();

        PdfDictionary catalog = pdf.GetCatalog().GetPdfObject();
        PdfDictionary? perms = catalog.GetAsDictionary(PdfName.Perms);
        PdfDictionary? docMdp = perms?.GetAsDictionary(PdfName.DocMDP);

        CertificationLevel = 0;

        PdfArray? references = docMdp?.GetAsArray(PdfName.Reference);

        if (references is null)
            return;

        for (int i = 0; i < references.Size(); i++)
        {
            PdfDictionary? reference = references.GetAsDictionary(i);

            if (reference is null)
                continue;

            PdfName? method = reference.GetAsName(PdfName.TransformMethod);

            if (!PdfName.DocMDP.Equals(method))
                continue;

            PdfDictionary? parameters = reference.GetAsDictionary(PdfName.TransformParams);
            PdfNumber? permission = parameters?.GetAsNumber(PdfName.P);

            // PDF DocMDP permission levels:
            // 1 = No changes
            // 2 = Form filling and signatures
            // 3 = Level 2 plus annotation changes
            CertificationLevel = permission?.IntValue() ?? 2;

            break;
        }
    }

    internal PdfDocument OpenDocument()
    {
        MemoryStream stream = new(_pdfBytes, writable: false);
        PdfReader reader = new(stream);

        return new PdfDocument(reader);
    }
}
