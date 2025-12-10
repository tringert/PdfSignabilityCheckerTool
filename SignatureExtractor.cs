using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Signatures;

namespace PdfSignabilityCheckerTool;

internal static class SignatureExtractor
{
    private static readonly Dictionary<PdfSignatureDictionary, List<PdfSignatureField>> _signaturesDictionary = [];
    private static IDictionary<string, PdfFormField> _acroFields = new Dictionary<string, PdfFormField>();
    private static readonly Dictionary<int, PdfSignatureDictionary> _pdfObjectDictMap = [];

    public static Dictionary<PdfSignatureDictionary, List<PdfSignatureField>> ExtractSigDictionaries(PdfReaderWrapper pdfReaderWrapper)
    {
        PdfDocument pdf = new(pdfReaderWrapper.GetPdfReader());
        PdfAcroForm acroForm = PdfAcroForm.GetAcroForm(pdf, false);

        if (acroForm == null)
            return _signaturesDictionary;

        SignatureUtil sigUtil = new(pdf);
        IList<string> signedFieldNames = sigUtil.GetSignatureNames();
        _acroFields = acroForm.GetAllFormFields();

        foreach (var name in signedFieldNames)
        {
            AddSignatureFieldToList(name);
        }

        return _signaturesDictionary;
    }

    private static void AddSignatureFieldToList(string name)
    {
        PdfFormField acroField = _acroFields[name];
        PdfWidgetAnnotation widget = acroField.GetWidgets()[0];
        PdfDictionary pdfField = widget.GetPdfObject();

        PdfSignatureField pdfSignatureField = new(pdfField);

        int refNumber = 0;
        PdfObject vObj = pdfField.Get(PdfName.V);

        if (vObj is PdfIndirectReference indirectObject)
        {
            refNumber = indirectObject.GetObjNumber();
        }
        else
        {
            // Sometimes signature dictionary is embedded directly as a PdfDictionary
            // but in that case GetAsDictionary(PdfName.V) will return it and obj number remains 0.
        }

        _pdfObjectDictMap.TryGetValue(refNumber, out PdfSignatureDictionary? signature);

        if (signature is null)
        {
            try
            {
                PdfDictionary? dictionary = pdfField.GetAsDictionary(PdfName.V);
                if (dictionary is null && vObj is PdfIndirectReference ir)
                {
                    // If /V is an indirect reference, we can also resolve it via the reader
                    PdfObject resolved = ir.GetRefersTo();
                    dictionary = resolved as PdfDictionary;
                }

                signature = new PdfSignatureDictionary(dictionary!);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Unable to create a PdfSignatureDictionary for field with name '{name}': {e.Message}");
                return;
            }

            _signaturesDictionary[signature] = [pdfSignatureField];
            _pdfObjectDictMap[refNumber] = signature;
        }
        else
        {
            List<PdfSignatureField> fieldList = _signaturesDictionary[signature];
            fieldList.Add(pdfSignatureField);
            Console.Error.WriteLine($"More than one field refers to the same signature dictionary: {fieldList}!");
        }
    }
}
