using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;
using PdfSignabilityCheckerTool.Enums;

namespace PdfSignabilityCheckerTool;

internal class PdfSignabilityChecker
{
    private readonly PdfReaderWrapper _pdfReaderWrapper;
    private readonly string _signatureFieldId;

    public PdfSignabilityChecker(MemoryStream ms, string signatureFieldId)
    {
        ms.Position = 0;

        _pdfReaderWrapper = new PdfReaderWrapper(ms);
        _signatureFieldId = signatureFieldId ?? "";
    }

    internal bool IsSignable(bool treatUnencryptedAsSignable)
    {
        // Preserve the original explicit override behavior.
        bool treatOpenedWithFullPermissionAsSignable =
            (!_pdfReaderWrapper.IsEncrypted || _pdfReaderWrapper.IsOpenedWithFullPermission)
            && treatUnencryptedAsSignable;

        if (treatOpenedWithFullPermissionAsSignable)
            return true;

        (bool isSignable, string reason) = CheckDocumentProperties();

        if (!isSignable)
        {
            Console.Error.WriteLine(reason);
            return false;
        }

        return true;
    }

    private (bool, string) CheckDocumentProperties()
    {
        using PdfDocument pdf = _pdfReaderWrapper.OpenDocument();

        (bool isDocPermissionsOk, string reason) = CheckDocumentPermissions(pdf);

        if (!isDocPermissionsOk)
            return (false, reason);

        (bool isDictPermissionsOk, string dictReason) = CheckSignatureRestrictionDictionaries(pdf);

        if (!isDictPermissionsOk)
            return (false, dictReason);

        return (true, "");
    }

    private (bool, string) CheckDocumentPermissions(PdfDocument pdf)
    {
        if (!CanFillSignatureForm())
        {
            return (false, "PDF permissions do not allow filling or signing form fields.");
        }

        // Creating a new signature field requires different permissions
        // from signing an existing unsigned signature field.
        if (!IsExistingUnsignedSignatureField(pdf) && !CanCreateSignatureField())
        {
            return (false, "PDF permissions do not allow creating a new signature field.");
        }

        return (true, "");
    }

    private bool IsExistingUnsignedSignatureField(PdfDocument pdf)
    {
        if (string.IsNullOrWhiteSpace(_signatureFieldId))
            return false;

        PdfAcroForm? acroForm = PdfAcroForm.GetAcroForm(pdf, false);

        if (acroForm is null)
            return false;

        IDictionary<string, PdfFormField> fields = acroForm.GetAllFormFields();

        if (!fields.TryGetValue(_signatureFieldId, out PdfFormField? field))
            return false;

        PdfDictionary fieldDictionary = field.GetPdfObject();

        // /FT can be inherited from a parent field.
        PdfName? fieldType = fieldDictionary.GetAsName(PdfName.FT);
        PdfDictionary? parent = fieldDictionary.GetAsDictionary(PdfName.Parent);

        while (fieldType is null && parent is not null)
        {
            fieldType = parent.GetAsName(PdfName.FT);
            parent = parent.GetAsDictionary(PdfName.Parent);
        }

        if (!PdfName.Sig.Equals(fieldType))
            return false;

        // An existing signature value means the field is already signed.
        PdfObject? value = fieldDictionary.Get(PdfName.V);

        return value is null || value.IsNull();
    }

    private (bool, string) CheckSignatureRestrictionDictionaries(PdfDocument pdf)
    {
        CertificationPermission? certificationPermission = GetCertificationPermission();

        if (IsDocumentChangeForbidden(certificationPermission))
            return (false, "DocMDP dictionary does not permit a new signature.");

        Dictionary<PdfSignatureDictionary, List<PdfSignatureField>> sigDictionaries = SignatureExtractor.ExtractSigDictionaries(pdf);

        // Check FieldMDP restrictions.
        foreach (PdfSignatureDictionary signatureDictionary in sigDictionaries.Keys)
        {
            SigFieldPermissions? fieldMdp = signatureDictionary.GetFieldMDP();

            if (fieldMdp is not null && IsSignatureFieldCreationForbidden(fieldMdp, _signatureFieldId))
                return (false, "FieldMDP dictionary does not permit the requested signature.");
        }

        // Check /Lock dictionaries on previously signed fields.
        foreach (List<PdfSignatureField> signatureFields in sigDictionaries.Values)
        {
            foreach (PdfSignatureField signatureField in signatureFields)
            {
                SigFieldPermissions? lockDict = signatureField.GetLockDictionary();

                if (lockDict is null)
                    continue;

                if (IsSignatureFieldCreationForbidden(lockDict, _signatureFieldId))
                    return (false, "Signature field /Lock dictionary does not permit the requested signature.");
            }
        }

        return (true, "");
    }

    private bool IsSignatureFieldCreationForbidden(SigFieldPermissions sigFieldPermissions, string signatureFieldId)
    {
        switch (sigFieldPermissions.Action)
        {
            case PdfLockAction.ALL:
                return true;

            case PdfLockAction.INCLUDE:
                if (!string.IsNullOrWhiteSpace(signatureFieldId) && sigFieldPermissions.Fields.Contains(signatureFieldId))
                    return true;

                break;

            case PdfLockAction.EXCLUDE:
                if (string.IsNullOrWhiteSpace(signatureFieldId))
                    return true;

                if (!sigFieldPermissions.Fields.Contains(signatureFieldId))
                    return true;

                break;

            default:
                throw new NotSupportedException($"Unsupported FieldMDP action: {sigFieldPermissions.Action}");
        }

        return IsDocumentChangeForbidden(GetCertificationPermission());
    }

    private static bool IsDocumentChangeForbidden(
        CertificationPermission? certificationPermission)
    {
        if (certificationPermission is null)
            return false;

        return certificationPermission == CertificationPermission.NO_CHANGE_PERMITTED;
    }

    public CertificationPermission? GetCertificationPermission()
    {
        int certificationLevel = GetCertificationLevel();

        if (certificationLevel <= 0)
            return null;

        return Helpers.GetCertificationPermissionByNumber(certificationLevel);
    }

    public int GetCertificationLevel()
    {
        return _pdfReaderWrapper.CertificationLevel;
    }

    private bool CanFillSignatureForm()
    {
        if (_pdfReaderWrapper.IsOpenedWithFullPermission)
            return true;

        int permissions = _pdfReaderWrapper.Permissions;

        return IsAllowModifyAnnotations(permissions) || IsAllowFillIn(permissions);
    }

    private bool CanCreateSignatureField()
    {
        if (_pdfReaderWrapper.IsOpenedWithFullPermission)
            return true;

        int permissions = _pdfReaderWrapper.Permissions;

        return IsAllowModifyContents(permissions)
            && IsAllowModifyAnnotations(permissions);
    }

    private static bool IsAllowModifyContents(int permissions)
    {
        return IsPermissionBitPresent(permissions, EncryptionConstants.ALLOW_MODIFY_CONTENTS);
    }

    private static bool IsAllowModifyAnnotations(int permissions)
    {
        return IsPermissionBitPresent(permissions, EncryptionConstants.ALLOW_MODIFY_ANNOTATIONS);
    }

    private static bool IsAllowFillIn(int permissions)
    {
        return IsPermissionBitPresent(permissions, EncryptionConstants.ALLOW_FILL_IN);
    }

    private static bool IsPermissionBitPresent(int permissions, int permissionBit)
    {
        return (permissions & permissionBit) == permissionBit;
    }
}
