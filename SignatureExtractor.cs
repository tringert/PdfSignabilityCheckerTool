
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Signatures;

namespace PdfSignabilityCheckerTool;

internal static class SignatureExtractor
{
    public static Dictionary<PdfSignatureDictionary, List<PdfSignatureField>> ExtractSigDictionaries(PdfReaderWrapper pdfReaderWrapper)
    {
        Dictionary<PdfSignatureDictionary, List<PdfSignatureField>> signatures = [];
        Dictionary<int, PdfSignatureDictionary> objectMap = [];

        PdfDocument pdf = new(pdfReaderWrapper.GetPdfReader());

        PdfAcroForm? acroForm = PdfAcroForm.GetAcroForm(pdf, false);

        if (acroForm is null)
            return signatures;

        SignatureUtil sigUtil = new(pdf);

        IList<string> signedFieldNames = sigUtil.GetSignatureNames();

        IDictionary<string, PdfFormField> acroFields =
            acroForm.GetAllFormFields();

        foreach (string name in signedFieldNames)
        {
            if (!acroFields.TryGetValue(name, out PdfFormField? acroField))
            {
                Console.Error.WriteLine(
                    $"Signature field '{name}' not found in AcroForm.");
                continue;
            }

            PdfDictionary? dictionary = sigUtil.GetSignatureDictionary(name);

            if (dictionary is null)
            {
                Console.Error.WriteLine(
                    $"Signature dictionary not found for field '{name}'.");
                continue;
            }

            IList<PdfWidgetAnnotation> widgets = acroField.GetWidgets();

            if (widgets.Count == 0)
            {
                Console.Error.WriteLine(
                    $"No widget found for signature field '{name}'.");
                continue;
            }

            PdfIndirectReference? reference =
                dictionary.GetIndirectReference();

            PdfSignatureDictionary? signature = null;

            if (reference is not null)
            {
                objectMap.TryGetValue(
                    reference.GetObjNumber(),
                    out signature);
            }

            if (signature is null)
            {
                signature = new PdfSignatureDictionary(dictionary);
                signatures[signature] = [];

                if (reference is not null)
                {
                    objectMap[reference.GetObjNumber()] = signature;
                }
            }

            foreach (PdfWidgetAnnotation widget in widgets)
            {
                PdfSignatureField pdfSignatureField =
                    new(widget.GetPdfObject());

                signatures[signature].Add(pdfSignatureField);
            }
        }

        return signatures;
    }
}
