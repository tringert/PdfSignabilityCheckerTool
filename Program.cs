using System.Diagnostics;
using iText.Kernel.Exceptions;

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

        try
        {
            PdfSignabilityChecker pdfSignabilityChecker = new(ms, GetSigFieldName(args));
            isSignable = pdfSignabilityChecker.IsSignable(treatUnencryptedAsSignable);
        }
        catch (BadPasswordException)
        {
            Console.Error.WriteLine(Constants.PasswordProtectedErrorMessage);
            WriteFalse();
            return 2;
        }
        catch (PdfException ex) when (ex.Message.Contains("Certificate is not provided"))
        {
            Console.Error.WriteLine(Constants.CertificateProtectedErrorMessage);
            WriteFalse();
            return 66;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex}");
            WriteFalse();
            return 99;
        }

        stdin.Close();

        Console.Write(isSignable
            ? "true"
            : "false");

        return isSignable ? 0 : 1;
    }

    private static string GetSigFieldName(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("-sigfieldname", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }

        return "";
    }

    private static void WriteFalse()
    {
        Console.Write("false");
    }
}
