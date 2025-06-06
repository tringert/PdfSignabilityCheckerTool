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
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("false");
                return 99;
            }
        }

        Console.WriteLine(isSignable
            ? "true"
            : "false");

        return isSignable ? 0 : 3;
    }
}
