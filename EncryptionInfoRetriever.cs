using iText.Kernel.Pdf;

namespace PdfSignabilityCheckerTool;

internal static class EncryptionInfoRetriever
{
    public static void PrintInfo(PdfReader reader)
    {
        PdfDocument pdf;

        pdf = new(reader);

        PdfDictionary encrypt = pdf.GetTrailer().GetAsDictionary(PdfName.Encrypt);

        if (encrypt == null)
        {
            Console.WriteLine("No /Encrypt dictionary (PDF NOT encrypted).");
            return;
        }

        Console.WriteLine();
        Console.WriteLine("== Encryption Dictionary ==");
        foreach (KeyValuePair<PdfName, PdfObject> entry in encrypt.EntrySet())
        {
            Console.WriteLine($"{entry.Key} : {entry.Value}");
        }

        Console.WriteLine();
        Console.WriteLine($"Raw permissions = {reader.GetPermissions()}");
        Console.WriteLine();
    }
}
