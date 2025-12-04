using System.Diagnostics;
using iText.Kernel.Exceptions;
using iText.Kernel.Pdf;

namespace PdfSignabilityCheckerTool;

internal static class Program
{
    static int Main(string[] args)
    {
        bool treatUnencryptedAsSignable = args.Contains(Constants.TreatUnencryptedAsSignable);
        bool isDebug = Debugger.IsAttached;

        if (!Console.IsInputRedirected && !isDebug)
        {
            Console.Error.WriteLine(Constants.NoInputErrorMessage);
            Environment.Exit(1);
        }

        Stream stdin;

        if (isDebug)
        {
            string? filePath = null;
            Debugger.Break();
            stdin = new MemoryStream(File.ReadAllBytes(filePath!));
        }
        else
        {
            stdin = Console.OpenStandardInput();
        }

        bool isSignable;

        using var ms = new MemoryStream();
        stdin.CopyTo(ms);

        // For debugging purposes only
        if (false)
        {

            ms.Position = 0;
            using PdfReader reader = new(ms);
            PermissionRetriever.PrintPermissions(reader);
            return 0;
        }

        try
        {
            if (args.Contains(Constants.EncryptionInfoParameterName))
            {
                ms.Position = 0;
                using PdfReader reader = new(ms);
                EncryptionInfoRetriever.PrintInfo(reader);
                return 0;
            }

            PdfSignabilityChecker pdfSignabilityChecker = new(ms);
            isSignable = pdfSignabilityChecker.IsSignable(treatUnencryptedAsSignable);
        }
        catch (BadPasswordException)
        {
            Console.Error.WriteLine(Constants.PasswordProtectedErrorMessage);
            Console.Write("false");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex}");
            Console.Write("false");
            return 99;
        }

        stdin.Close();

        Console.Write(isSignable
            ? "true"
            : "false");

        return isSignable ? 0 : 1;
    }
}
