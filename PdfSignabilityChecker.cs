using iText.Kernel.Pdf;

namespace PdfSignabilityCheckerTool;

internal static class PdfSignabilityChecker
{
    internal static bool IsSignable(PdfReader reader, bool treatUnencryptedAsSignable)
    {
        using PdfDocument pdfDoc = new(reader);

        if (!reader.IsEncrypted() && treatUnencryptedAsSignable)
            return true;

        uint perms = unchecked((uint)reader.GetPermissions());

        bool modifyContentsAllowed = (perms & Constants.PermissionFlags.AllowModifyContents) != 0;

        return modifyContentsAllowed;
    }
}
