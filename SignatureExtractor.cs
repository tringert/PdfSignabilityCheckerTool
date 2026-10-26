using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Signatures;

namespace PdfSignabilityCheckerTool;

internal static class SignatureExtractor
{
    public static Dictionary<PdfSignatureDictionary, List<PdfSignatureField>> ExtractSigDictionaries(PdfDocument pdf)
    {
        Dictionary<PdfSignatureDictionary, List<PdfSignatureField>> signatures = [];
        Dictionary<int, PdfSignatureDictionary> objectMap = [];

        PdfAcroForm? acroForm = PdfAcroForm.GetAcroForm(pdf, false);

        if (acroForm is null)
            return signatures;

        SignatureUtil signatureUtil = new(pdf);
        IDictionary<string, PdfFormField> fields = acroForm.GetAllFormFields();

        foreach (string name in signatureUtil.GetSignatureNames())
        {
            bool cantGetSignature = !TryGetSignature(signatureUtil, fields, name,
                    out PdfDictionary? dictionary,
                    out IList<PdfWidgetAnnotation>? widgets);

            if (cantGetSignature)
                continue;

            PdfSignatureDictionary signature = GetOrCreateSignature(dictionary!, signatures, objectMap);
            List<PdfSignatureField> signatureFields = signatures[signature];

            foreach (PdfWidgetAnnotation widget in widgets!)
            {
                signatureFields.Add(new PdfSignatureField(widget.GetPdfObject()));
            }
        }

        return signatures;
    }

    private static bool TryGetSignature(
        SignatureUtil signatureUtil,
        IDictionary<string, PdfFormField> fields,
        string name,
        out PdfDictionary? dictionary,
        out IList<PdfWidgetAnnotation>? widgets)
    {
        dictionary = null;
        widgets = null;

        if (!fields.TryGetValue(name, out PdfFormField? field))
        {
            Console.Error.WriteLine($"Signature field '{name}' not found in AcroForm.");
            return false;
        }

        dictionary = signatureUtil.GetSignatureDictionary(name);

        if (dictionary is null)
        {
            Console.Error.WriteLine($"Signature dictionary not found for field '{name}'.");
            return false;
        }

        widgets = field.GetWidgets();

        if (widgets.Count == 0)
        {
            Console.Error.WriteLine($"No widget found for signature field '{name}'.");
            return false;
        }

        return true;
    }

    private static PdfSignatureDictionary GetOrCreateSignature(
        PdfDictionary dictionary,
        Dictionary<PdfSignatureDictionary, List<PdfSignatureField>> signatures,
        Dictionary<int, PdfSignatureDictionary> objectMap)
    {
        PdfIndirectReference? reference = dictionary.GetIndirectReference();
        bool hasDictionary = objectMap.TryGetValue(reference.GetObjNumber(), out PdfSignatureDictionary? existing);

        if (reference is not null && hasDictionary)
            return existing!;

        PdfSignatureDictionary signature = new(dictionary);
        signatures.Add(signature, []);

        if (reference is not null)
            objectMap.Add(reference.GetObjNumber(), signature);

        return signature;
    }
}
