namespace PdfSignabilityCheckerTool;

internal static class Constants
{
    internal const string TreatUnencryptedAsSignable = "--unencrypted-is-signable";
    internal const string EncryptionInfoParameterName = "--encinfo";
    internal const string PasswordProtectedErrorMessage = "This PDF is password-protected and therefore cannot be opened.";
    internal const string NoInputErrorMessage = $@"
No input was redirected into the program.

Usage: Redirect a raw byte stream to the input of this application.
For example, in PowerShell:

Get-Content -Path ""C:\Temp\doc.pdf"" -AsByteStream -Raw | .\PdfSignabilityCheckerTool.exe

Parameters:
{TreatUnencryptedAsSignable}     Treat unencrypted PDFs as signable.
{EncryptionInfoParameterName}                     Display the encryption dictionary.
";

    internal static class PermissionFlags
    {
        internal const int AllowPrinting = 1 << 2;
        internal const int AllowModifyContents = 1 << 3;
        internal const int AllowCopy = 1 << 4;
        internal const int AllowModifyAnnotations = 1 << 5;
        internal const int AllowFillIn = 1 << 8;
        internal const int AllowScreenReaders = 1 << 9;
        internal const int AllowAssembly = 1 << 10;
        internal const int AllowDegradedPrinting = 1 << 11;
    }
}
