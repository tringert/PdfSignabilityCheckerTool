using iText.Kernel.Pdf;
using PdfSignabilityCheckerTool.Enums;
using static PdfSignabilityCheckerTool.PdfSignabilityChecker;

namespace PdfSignabilityCheckerTool;

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
