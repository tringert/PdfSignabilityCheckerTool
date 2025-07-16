using iText.Kernel.Exceptions;

namespace PdfSignabilityCheckerTool;

internal static class Program
{
    static int Main()
    {
        if (!Console.IsInputRedirected)
        {
            Console.Error.WriteLine("No input was redirected into the program.");
            Environment.Exit(1);
        }

        using Stream stdin = Console.OpenStandardInput();
        bool isSignable;

        using (var ms = new MemoryStream())
        {
            stdin.CopyTo(ms);
            ms.Position = 0;

            try
            {
                isSignable = PdfSignabilityChecker.IsSignable(ms);
            }
            catch (BadPasswordException)
            {
                Console.Error.WriteLine("This PDF is password-protected and therefore cannot be signed.");
                Console.Write("false");
                return 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex}");
                Console.Write("false");
                return 99;
            }
        }

        Console.Write(isSignable
            ? "true"
            : "false");

        return isSignable ? 0 : 1;
    }
}
