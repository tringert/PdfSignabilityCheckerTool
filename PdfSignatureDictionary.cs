
using iText.Kernel.Pdf;

namespace PdfSignabilityCheckerTool;

internal class PdfSignatureDictionary
{
    public PdfDictionary Dictionary { get; }

    public PdfSignatureDictionary(PdfDictionary dictionary)
    {
        Dictionary = dictionary ??
            throw new ArgumentNullException(nameof(dictionary));
    }

    public SigFieldPermissions? GetFieldMDP()
    {
        PdfArray? referenceArray =
            Dictionary.GetAsArray(PdfName.Reference);

        if (referenceArray is null)
            return null;

        for (int i = 0; i < referenceArray.Size(); i++)
        {
            PdfDictionary? refEntry =
                referenceArray.GetAsDictionary(i);

            if (refEntry is null)
                continue;

            PdfName? transformMethod =
                refEntry.GetAsName(PdfName.TransformMethod);

            if (!PdfName.FieldMDP.Equals(transformMethod))
                continue;

            PdfDictionary? transformParams =
                refEntry.GetAsDictionary(PdfName.TransformParams);

            if (transformParams is null)
                continue;

            PdfName? action =
                transformParams.GetAsName(PdfName.Action);

            if (action is null)
            {
                Console.Error.WriteLine(
                    "FieldMDP transform is missing the /Action entry.");
                continue;
            }

            PdfArray? fields =
                transformParams.GetAsArray(PdfName.Fields);

            return new SigFieldPermissions(action, fields);
        }

        return null;
    }
}
