namespace PdfSignabilityCheckerTool;

internal static class Constants
{
    internal const string TreatUnencryptedAsSignable = "--unencrypted-is-signable";
    internal const string PasswordProtectedErrorMessage = "This PDF is password-protected and therefore cannot be opened.";
    internal const string CertificateProtectedErrorMessage = "The PDF is encrypted with a certificate and cannot be opened without the private key.";
    internal const string NoInputErrorMessage = $@"
No input was redirected into the program.

Usage: Redirect a raw byte stream to the input of this application.
For example, in PowerShell:

Get-Content -Path ""C:\Temp\doc.pdf"" -AsByteStream -Raw | .\PdfSignabilityCheckerTool.exe [parameter]

Available parameters:
{TreatUnencryptedAsSignable}     Treat unencrypted PDFs as signable.
-sigfieldname <name>          Name of the signature field. When present, used to evaluate FieldMDP/Lock dictionaries that reference specific field names.
";
}
