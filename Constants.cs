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
}
