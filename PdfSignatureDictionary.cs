using iText.Kernel.Pdf;

namespace PdfSignabilityCheckerTool;

internal class PdfSignatureDictionary
{
    public PdfDictionary Dictionary { get; }

    public PdfSignatureDictionary(PdfDictionary dictionary)
    {
        Dictionary = dictionary
            ?? throw new ArgumentNullException(nameof(dictionary));
    }

    public SigFieldPermissions? GetFieldMDP()
    {
        PdfArray? referenceArray = Dictionary.GetAsArray(PdfName.Reference);

        if (referenceArray is null)
            return null;

        for (int i = 0; i < referenceArray.Size(); i++)
        {
            PdfDictionary? refEntry = referenceArray.GetAsDictionary(i);

            if (refEntry is null)
                continue;

            PdfName? transformMethod = refEntry.GetAsName(PdfName.TransformMethod);

            if (!PdfName.FieldMDP.Equals(transformMethod))
                continue;

            PdfDictionary? transformParams = refEntry.GetAsDictionary(PdfName.TransformParams)
                ?? throw new InvalidDataException("FieldMDP reference is missing /TransformParams.");
            PdfName? action = transformParams.GetAsName(PdfName.Action)
                ?? throw new InvalidDataException("FieldMDP transform is missing /Action.");
            PdfArray? fields = transformParams.GetAsArray(PdfName.Fields);

            if ((PdfName.Include.Equals(action) || PdfName.Exclude.Equals(action))
                && fields is null)
            {
                throw new InvalidDataException($"FieldMDP action {action} requires a /Fields array.");
            }

            return new SigFieldPermissions(action, fields);
        }

        return null;
    }
}
