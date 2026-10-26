
using iText.Kernel.Pdf;
using PdfSignabilityCheckerTool.Enums;

namespace PdfSignabilityCheckerTool;

internal class SigFieldPermissions
{
    public PdfLockAction Action { get; set; }

    public List<string> Fields { get; set; } = [];

    public CertificationPermission? CertificationPermission { get; set; }

    public SigFieldPermissions()
    {
    }

    public SigFieldPermissions(PdfName actionName, PdfArray? fieldsArray)
    {
        if (PdfName.All.Equals(actionName))
        {
            Action = PdfLockAction.ALL;
        }
        else if (PdfName.Include.Equals(actionName))
        {
            Action = PdfLockAction.INCLUDE;
        }
        else if (PdfName.Exclude.Equals(actionName))
        {
            Action = PdfLockAction.EXCLUDE;
        }
        else
        {
            throw new NotSupportedException(
                $"Unknown FieldMDP action: {actionName}");
        }

        Fields = [];

        if (fieldsArray is null)
            return;

        for (int i = 0; i < fieldsArray.Size(); i++)
        {
            PdfString? fieldName = fieldsArray.GetAsString(i);

            if (fieldName is not null)
            {
                Fields.Add(fieldName.ToUnicodeString());
            }
        }
    }
}
