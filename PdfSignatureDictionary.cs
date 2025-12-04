using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Signatures;
using static PdfSignabilityCheckerTool.PdfSignabilityChecker;

namespace PdfSignabilityCheckerTool;

public enum PdfLockAction
{
    ALL,
    INCLUDE,
    EXCLUDE
}

internal class SigFieldPermissions
{
    public PdfLockAction Action { get; set; }
    public List<string>? Fields { get; set; }
    public CertificationPermission? CertificationPermission { get; set; }

    public SigFieldPermissions()
    {

    }

    public SigFieldPermissions(PdfName actionName, PdfArray fieldsArray)
    {
        if (PdfName.All.Equals(actionName))
            Action = PdfLockAction.ALL;
        else if (PdfName.Include.Equals(actionName))
            Action = PdfLockAction.INCLUDE;
        else if (PdfName.Exclude.Equals(actionName))
            Action = PdfLockAction.EXCLUDE;
        else
            throw new NotSupportedException($"Unknown FieldMDP action: {actionName}");

        Fields = [];

        if (fieldsArray != null)
        {
            for (int i = 0; i < fieldsArray.Size(); i++)
            {
                PdfString s = fieldsArray.GetAsString(i);
                if (s != null)
                    Fields.Add(s.ToString());
            }
        }
    }
}

internal class PdfSignatureDictionary(PdfDictionary dict)
{
    public PdfDictionary Dictionary { get; } = dict;

    public SigFieldPermissions? GetFieldMDP()
    {
        PdfArray referenceArray = Dictionary.GetAsArray(PdfName.Reference);

        if (referenceArray is null)
            return null;

        for (int i = 0; i < referenceArray.Size(); i++)
        {
            PdfDictionary refEntry = referenceArray.GetAsDictionary(i);

            if (refEntry is null)
                continue;

            PdfName transformMethod = refEntry.GetAsName(PdfName.TransformMethod);

            if (transformMethod != PdfName.FieldMDP)
                continue;

            PdfDictionary transformParams = refEntry.GetAsDictionary(PdfName.TransformParams);

            if (transformParams is null)
                continue;

            PdfName action = transformParams.GetAsName(PdfName.Action);
            PdfArray fields = transformParams.GetAsArray(PdfName.Fields);

            return new SigFieldPermissions(action, fields);
        }

        return null;
    }
}

internal class PdfSignatureField(PdfDictionary dict)
{
    public PdfDictionary FieldDict { get; } = dict;

    public SigFieldPermissions? GetLockDictionary()
    {
        PdfDictionary lockDict = FieldDict.GetAsDictionary(PdfName.Lock);

        if (lockDict != null)
        {
            return ExtractPermissionsDictionary(lockDict);
        }

        return null;
    }

    public static SigFieldPermissions ExtractPermissionsDictionary(PdfDictionary dict)
    {
        SigFieldPermissions sigPerm = new();

        // Action
        string action = dict.GetAsName(PdfName.Action).GetValue();
        sigPerm.Action = GetPdfLockActionFromName(action);

        // Fields
        List<string> fields = [];
        PdfArray fieldsArray = dict.GetAsArray(PdfName.Fields);

        if (fieldsArray != null)
        {
            for (int i = 0; i < fieldsArray.Size(); i++)
            {
                string? fieldName = fieldsArray.GetAsString(i)?.ToString();

                if (fieldName != null)
                    fields.Add(fieldName);
            }
        }

        sigPerm.Fields = fields;

        // Certification permissions
        PdfName typeName = dict.GetAsName(PdfName.Type);

        if (typeName == PdfName.SigFieldLock)
        {
            int? num = dict.GetAsInt(PdfName.P);

            if (num != null)
                sigPerm.CertificationPermission = GetCertificationPermissionByNumber(num.Value);
        }

        return sigPerm;
    }

    public static PdfLockAction GetPdfLockActionFromName(string? name)
    {
        return name switch
        {
            "All" => PdfLockAction.ALL,
            "Include" => PdfLockAction.INCLUDE,
            "Exclude" => PdfLockAction.EXCLUDE,
            null or "" => throw new ArgumentNullException(nameof(name)),
            _ => throw new NotSupportedException($"Unknown action name: {name}")
        };
    }
}

internal static class SignatureExtractor
{
    public static Dictionary<PdfSignatureDictionary, List<PdfSignatureField>> ExtractSigDictionaries(PdfDocument pdf)
    {
        Dictionary<PdfSignatureDictionary, List<PdfSignatureField>> signatureDictionaryMap = [];
        Dictionary<int, PdfSignatureDictionary> pdfObjectDictMap = [];

        PdfAcroForm acroForm = PdfAcroForm.GetAcroForm(pdf, false);

        if (acroForm == null)
            return signatureDictionaryMap;

        SignatureUtil sigUtil = new(pdf);
        IList<string> signedFieldNames = sigUtil.GetSignatureNames();
        IDictionary<string, PdfFormField> acroFields = acroForm.GetAllFormFields();

        foreach (var name in signedFieldNames)
        {
            PdfFormField acroField = acroFields[name];
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

            pdfObjectDictMap.TryGetValue(refNumber, out PdfSignatureDictionary? signature);

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
                    Console.WriteLine($"Unable to create a PdfSignatureDictionary for field with name '{name}': {e.Message}");
                    continue;
                }

                signatureDictionaryMap[signature] = [pdfSignatureField];
                pdfObjectDictMap[refNumber] = signature;
            }
            else
            {
                List<PdfSignatureField> fieldList = signatureDictionaryMap[signature];
                fieldList.Add(pdfSignatureField);
                Console.WriteLine($"More than one field refers to the same signature dictionary: {fieldList}!");
            }
        }

        return signatureDictionaryMap;
    }
}
