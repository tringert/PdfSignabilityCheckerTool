using PdfSignabilityCheckerTool.Enums;

namespace PdfSignabilityCheckerTool;

internal static class Helpers
{
    public static CertificationPermission? GetCertificationPermissionByNumber(int certificationLevel)
    {
        return certificationLevel switch
        {
            1 => CertificationPermission.NO_CHANGE_PERMITTED,
            2 => CertificationPermission.MINIMAL_CHANGES_PERMITTED,
            3 => CertificationPermission.CHANGES_PERMITTED,
            _ => throw new InvalidDataException($"Not supported /DocMDP code value : {certificationLevel}"),
        };
    }
}
