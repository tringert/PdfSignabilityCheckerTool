using iText.Kernel.Pdf;
using PdfSignabilityCheckerTool.Enums;

namespace PdfSignabilityCheckerTool;

internal class PdfSignatureField(PdfDictionary dict)
{
    public PdfDictionary FieldDict { get; } = dict;

    public SigFieldPermissions? GetLockDictionary()
    {
        PdfDictionary lockDict = FieldDict.GetAsDictionary(PdfName.Lock);

        if (lockDict != null)
            return ExtractPermissionsDictionary(lockDict);

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
                sigPerm.CertificationPermission = Helpers.GetCertificationPermissionByNumber(num.Value);
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
