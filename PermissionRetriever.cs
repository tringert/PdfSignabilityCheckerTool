using iText.Forms;
using iText.Kernel.Pdf;

namespace PdfSignabilityCheckerTool;

internal static class PermissionRetriever
{
    public static void PrintPermissions(PdfReader reader)
    {
        using PdfDocument pdf = new(reader);

        CanModifyContents(pdf);

        Console.WriteLine();
        Console.WriteLine($"Is Encrypted: {(reader.IsEncrypted() ? "YES" : "NO")}");
        Console.WriteLine($"Is Opened with full permission: {(reader.IsOpenedWithFullPermission() ? "YES" : "NO")}");
        Console.WriteLine();

        PrintEncryptionInfo(reader, pdf);
        PrintEncryptionFlags(pdf);

        int rawPerms = reader.GetPermissions();
        uint perms = unchecked((uint)rawPerms);

        Console.WriteLine($"Raw P value: {rawPerms} ({perms})");
        Console.WriteLine("Permissions set:");

        PrintPermission(perms, EncryptionConstants.ALLOW_ASSEMBLY, "ALLOW_ASSEMBLY");
        PrintPermission(perms, EncryptionConstants.ALLOW_COPY, "ALLOW_COPY");
        PrintPermission(perms, EncryptionConstants.ALLOW_DEGRADED_PRINTING, "ALLOW_DEGRADED_PRINTING");
        PrintPermission(perms, EncryptionConstants.ALLOW_FILL_IN, "ALLOW_FILL_IN");
        PrintPermission(perms, EncryptionConstants.ALLOW_MODIFY_ANNOTATIONS, "ALLOW_MODIFY_ANNOTATIONS");
        PrintPermission(perms, EncryptionConstants.ALLOW_MODIFY_CONTENTS, "ALLOW_MODIFY_CONTENTS");
        PrintPermission(perms, EncryptionConstants.ALLOW_PRINTING, "ALLOW_PRINTING");
        PrintPermission(perms, EncryptionConstants.ALLOW_SCREENREADERS, "ALLOW_SCREENREADERS");
    }

    private static void PrintPermission(uint perms, int flag, string name)
    {
        bool allowed = (perms & flag) != 0;
        Console.WriteLine($"  {name}: {(allowed ? "YES" : "NO")}");
    }

    public static void PrintEncryptionInfo(PdfReader reader, PdfDocument pdf)
    {
        if (!reader.IsEncrypted())
        {
            Console.WriteLine("PDF is not encrypted.");
            return;
        }

        PdfDictionary encrypt = pdf.GetTrailer().GetAsDictionary(PdfName.Encrypt);
        if (encrypt == null)
        {
            Console.WriteLine("Encrypted = true, but no encryption dictionary found.");
            return;
        }

        Console.WriteLine("=== Encryption Info ===");

        int v = encrypt.GetAsNumber(PdfName.V)?.IntValue() ?? -1;
        int r = encrypt.GetAsNumber(PdfName.R)?.IntValue() ?? -1;

        Console.WriteLine($"V (Algorithm Version): {v}");
        Console.WriteLine($"R (Revision):          {r}");

        bool encryptMetadata = encrypt.GetAsBoolean(PdfName.EncryptMetadata)?.GetValue() ?? true;
        Console.WriteLine($"Encrypt Metadata:      {(encryptMetadata ? "YES" : "NO")}");

        // Default crypt filter
        PdfDictionary cf = encrypt.GetAsDictionary(PdfName.CF);
        PdfDictionary stdcf = cf?.GetAsDictionary(PdfName.StdCF)
                              ?? cf?.GetAsDictionary(new PdfName("DefaultCryptFilter"));

        string cfm = stdcf?.GetAsName(PdfName.CFM)?.GetValue() ?? "(unknown)";
        int? lengthBits = stdcf?.GetAsNumber(PdfName.Length)?.IntValue();

        Console.WriteLine($"Crypt Filter Method:   {cfm}");
        Console.WriteLine($"Key Length:            {lengthBits} bytes ({lengthBits * 8} bits)");

        Console.WriteLine();

        // Detect encryption type
        PrintEncryptionType(v, cfm);
        Console.WriteLine();
    }









    public static bool CanModifyContents(PdfDocument pdfDoc)
    {
        FieldMDPInfo mdp = GetFieldMDP(pdfDoc);

        // No signature lock → full permissions
        if (mdp == null || mdp.P == null)
            return true;

        // PDF spec: any P value means document contents cannot be modified
        return false;
    }

    public static bool CanModifyAnnotations(PdfDocument pdfDoc)
    {
        FieldMDPInfo mdp = GetFieldMDP(pdfDoc);

        if (mdp == null || mdp.P == null)
            return true;

        // P=3 = annotations allowed
        return mdp.P == 3;
    }

    private static FieldMDPInfo GetFieldMDP(PdfDocument pdfDoc)
    {
        PdfAcroForm form = PdfAcroForm.GetAcroForm(pdfDoc, false);

        if (form == null)
            return null;

        foreach (var field in form.GetAllFormFields())
        {
            var widgets = field.Value.GetWidgets();
            if (widgets == null)
                continue;

            foreach (var widget in widgets)
            {
                PdfDictionary lockDict = widget.GetPdfObject().GetAsDictionary(PdfName.Lock);
                if (lockDict == null)
                    continue;

                // Action (Fill, All, Include) — we return it but don’t use it yet
                PdfName action = lockDict.GetAsName(PdfName.Action);

                // P value = restriction level
                PdfNumber pNumber = lockDict.GetAsNumber(PdfName.P);

                return new FieldMDPInfo
                {
                    Action = action?.GetValue(),
                    P = pNumber?.IntValue()
                };
            }
        }

        return null;
    }

    private class FieldMDPInfo
    {
        public string Action { get; set; }

        public int? P { get; set; }
    }











    public static void PrintEncryptionFlags(PdfDocument pdf)
    {
        if (!pdf.GetReader().IsEncrypted())
        {
            Console.WriteLine("PDF is not encrypted.");
            pdf.Close();
            return;
        }

        PdfDictionary encryptDict = pdf.GetTrailer().GetAsDictionary(PdfName.Encrypt);
        if (encryptDict == null)
        {
            Console.WriteLine("No encryption dictionary found.");
            pdf.Close();
            return;
        }

        // --- DO_NOT_ENCRYPT_METADATA ---
        // /EncryptMetadata false
        PdfBoolean encryptMetadata = encryptDict.GetAsBoolean(PdfName.EncryptMetadata);
        bool doNotEncryptMetadata = (encryptMetadata != null && !encryptMetadata.GetValue());

        Console.WriteLine($"DO_NOT_ENCRYPT_METADATA: {(doNotEncryptMetadata ? "YES" : "NO")}");


        // --- EMBEDDED_FILES_ONLY ---
        // /EFF << ... >>
        PdfObject eff = encryptDict.Get(PdfName.EFF);
        bool embeddedFilesOnly = eff != null && eff.IsDictionary();

        Console.WriteLine($"EMBEDDED_FILES_ONLY: {(embeddedFilesOnly ? "YES" : "NO")}");
        Console.WriteLine();

        pdf.Close();
    }

    private static void PrintEncryptionType(int version, string cfm)
    {
        Console.WriteLine("=== Encryption Type Detection ===");

        if (cfm.Equals("AESV3", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Encryption: AES-256 (AESV3)");
            return;
        }

        if (cfm.Equals("AESV2", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Encryption: AES-128 (AESV2)");
            return;
        }

        if (cfm.Equals("AESGCM", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Encryption: AES-GCM (PDF 2.0)");
            return;
        }

        if (version == 1)
        {
            Console.WriteLine("Encryption: Standard 40-bit RC4");
            return;
        }

        if (version == 2)
        {
            Console.WriteLine("Encryption: Standard 128-bit RC4");
            return;
        }

        Console.WriteLine("Encryption: Unknown / Custom");
    }
}
