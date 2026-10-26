using iText.Kernel.Pdf;
using PdfSignabilityCheckerTool.Enums;

namespace PdfSignabilityCheckerTool;

internal class PdfSignatureField
{
    public PdfDictionary FieldDict { get; }

    public PdfSignatureField(PdfDictionary dict)
    {
        FieldDict = dict ??
            throw new ArgumentNullException(nameof(dict));
    }

    public SigFieldPermissions? GetLockDictionary()
    {
        PdfDictionary? lockDict = FieldDict.GetAsDictionary(PdfName.Lock);

        if (lockDict is null)
            return null;

        return ExtractPermissionsDictionary(lockDict);
    }

    public static SigFieldPermissions ExtractPermissionsDictionary(PdfDictionary dict)
    {
        ArgumentNullException.ThrowIfNull(dict);

        PdfName? actionName = dict.GetAsName(PdfName.Action)
            ?? throw new InvalidDataException("Signature field /Lock dictionary is missing /Action.");
        PdfLockAction action = GetPdfLockActionFromName(actionName.GetValue());
        PdfArray? fieldsArray = dict.GetAsArray(PdfName.Fields);

        if ((action == PdfLockAction.INCLUDE || action == PdfLockAction.EXCLUDE)
            && fieldsArray is null)
        {
            throw new InvalidDataException($"Signature field /Lock action {action} requires a /Fields array.");
        }

        SigFieldPermissions sigPerm = new(actionName, fieldsArray);

        PdfName? typeName = dict.GetAsName(PdfName.Type);

        if (typeName is not null && !PdfName.SigFieldLock.Equals(typeName))
            throw new InvalidDataException($"Unexpected signature lock dictionary type: {typeName}");

        PdfNumber? permissionNumber = dict.GetAsNumber(PdfName.P);

        if (permissionNumber is not null)
        {
            int level = permissionNumber.IntValue();

            if (level is < 1 or > 3)
                throw new InvalidDataException($"Invalid signature lock certification permission: {level}");

            sigPerm.CertificationPermission = Helpers.GetCertificationPermissionByNumber(level);
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
            _ => throw new NotSupportedException(
                $"Unknown signature lock action: {name}")
        };
    }
}
