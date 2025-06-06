using iText.Kernel.Pdf;

namespace PdfSignabilityCheckerTool;

internal static class PdfSignabilityChecker
{
    internal static bool IsSignable(MemoryStream ms)
    {
        using PdfReader reader = new(ms);
        using PdfDocument pdfDoc = new(reader);
        PdfReader readerWithPermissions = pdfDoc.GetReader();
        bool isEncrypted = readerWithPermissions.IsEncrypted();

        if (!isEncrypted)
            return true;

        if (!CanCreateSignatureFieldDespiteEncryption(readerWithPermissions))
            return false;

        return true;
    }

    private static bool CanCreateSignatureFieldDespiteEncryption(PdfReader readerWithPermissions)
    {
        int rawPermissions = readerWithPermissions.GetPermissions();
        uint permissions = unchecked((uint)rawPermissions);
        bool isContentModificationsAllowed = HasPermission(permissions, EncryptionConstants.ALLOW_MODIFY_CONTENTS);
        bool isAnnotationModificationsAllowed = HasPermission(permissions, EncryptionConstants.ALLOW_MODIFY_ANNOTATIONS);

        return isContentModificationsAllowed && isAnnotationModificationsAllowed;
    }

    private static bool HasPermission(uint permissions, int permissionFlag)
    {
        return (permissions & permissionFlag) != 0;
    }
}
