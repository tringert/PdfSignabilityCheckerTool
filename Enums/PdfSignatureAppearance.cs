namespace PdfSignabilityCheckerTool.Enums;

public enum PdfSignatureAppearance
{
    NOT_CERTIFIED = -1,
    CERTIFIED_ALL_CHANGES_ALLOWED = 0,
    CERTIFIED_NO_CHANGES_ALLOWED = 1,
    CERTIFIED_FORM_FILLING = 2,
    CERTIFIED_FORM_FILLING_AND_ANNOTATIONS = 3
}
