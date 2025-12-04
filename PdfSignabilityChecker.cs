using iText.Kernel.Pdf;

namespace PdfSignabilityCheckerTool;

internal class PdfSignabilityChecker
{
    private PdfReaderWrapper _pdfReaderWrapper;

    public PdfSignabilityChecker(MemoryStream ms)
    {
        ms.Position = 0;
        _pdfReaderWrapper = new(ms);
    }

    private PdfDocument GetPdfDocument()
    {
        PdfReader reader = _pdfReaderWrapper.GetPdfReader();
        return new PdfDocument(reader);
    }

    internal bool IsSignable(bool treatUnencryptedAsSignable)
    {
        bool treatOpenedWithFullPermissionAsSignable = (!_pdfReaderWrapper.IsEncrypted || _pdfReaderWrapper.IsOpenedWithFullPermission) && treatUnencryptedAsSignable;

        if (treatOpenedWithFullPermissionAsSignable)
            return true;

        (bool isSignable, string reason) result = NewWay();

        PdfDictionary encryptDict = _pdfReaderWrapper.Trailer.GetAsDictionary(PdfName.Encrypt);

        if (encryptDict != null)
        {
            // encryption dictionary exists but is invalid or empty
            PdfNumber p = encryptDict.GetAsNumber(PdfName.P);
            if (p != null)
            {
                int permissionBits = p.IntValue();

                bool modifyAllowed = (permissionBits & -4) != 0; // example bit test
                                                                 // … you can manually decode /P here

                return modifyAllowed;
            }
        }

        uint perms = unchecked((uint)_pdfReaderWrapper.Permissions);

        bool modifyContentsAllowed = (perms & EncryptionConstants.ALLOW_MODIFY_CONTENTS) != 0;
        bool modifyAnnotationsAllowed = (perms & EncryptionConstants.ALLOW_MODIFY_ANNOTATIONS) != 0;

        return modifyContentsAllowed && modifyAnnotationsAllowed;
    }

    private (bool, string) NewWay()
    {
        (bool isDocPermissionsOk, string reason) = CheckDocumentPermissions();

        if (!isDocPermissionsOk)
        {
            return (false, reason);
        }

        (bool isDictPermissionsOk, string dictReason) = CheckSignatureRestrictionDictionaries();

        if (!isDictPermissionsOk)
        {
            return (false, dictReason);
        }

        return (true, "");
    }

    private (bool, string) CheckDocumentPermissions()
    {
        bool canFillSignatureForms = CanFillSignatureForm();

        if (!canFillSignatureForms)
        {
            return (false, "PDF Permissions dictionary does not allow fill in interactive form fields, including existing signature fields when document is open with user-access!");
        }

        bool canCreateSignatureField = CanCreateSignatureField();

        if (!canCreateSignatureField)
        {
            return (false, "PDF Permissions dictionary does not allow modification or creation interactive form fields, including signature fields when document is open with user-access!");
        }

        return (true, "");
    }

    private (bool, string) CheckSignatureRestrictionDictionaries()
    {
        CertificationPermission? certificationPermission = GetCertificationPermission();

        if (IsDocumentChangeForbidden(certificationPermission))
        {
            return (false, "DocMDP dictionary does not permit a new signature creation!");
        }

        try
        {
            // Honnan tudjuk, hogy milyen Id-t használ a KEASZ?
            string signatureFieldId = "";//fieldParameters.getFieldId();

            Dictionary<PdfSignatureDictionary, List<PdfSignatureField>> sigDictionaries = SignatureExtractor.ExtractSigDictionaries(GetPdfDocument());

            // FieldMDP
            foreach (PdfSignatureDictionary signatureDictionary in sigDictionaries.Keys)
            {
                SigFieldPermissions? fieldMDP = signatureDictionary.GetFieldMDP();

                if (fieldMDP != null && IsSignatureFieldCreationForbidden(fieldMDP, signatureFieldId))
                {
                    return (false, "FieldMDP dictionary does not permit a new signature creation!");
                }
            }

            // Lock dictionary
            foreach (List<PdfSignatureField> signatureFieldList in sigDictionaries.Values)
            {
                foreach (PdfSignatureField signatureField in signatureFieldList)
                {
                    SigFieldPermissions? lockDict = signatureField.GetLockDictionary();

                    if (lockDict != null && lockDict.CertificationPermission != null &&
                        IsSignatureFieldCreationForbidden(lockDict, signatureFieldId))
                    {
                        return (false, "Lock dictionary does not permit a new signature creation!");
                    }
                }
            }

        }
        catch (IOException e)
        {
            return (false, $"An error occurred while reading signature dictionary entries : {e}");
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
                if (string.IsNullOrWhiteSpace(signatureFieldId))
                    return false;

                if (sigFieldPermissions.Fields is null)
                    return false;

                if (sigFieldPermissions.Fields.Contains(signatureFieldId))
                    return true;

                break;

            case PdfLockAction.EXCLUDE:
                if (string.IsNullOrWhiteSpace(signatureFieldId))
                    return true;

                if (sigFieldPermissions.Fields is null)
                    return true;

                if (!sigFieldPermissions.Fields.Contains(signatureFieldId))
                    return true;
                break;

            default:
                throw new NotSupportedException(
                    $"The action value '{sigFieldPermissions.Action}' is not supported!");
        }

        CertificationPermission? certPerm = GetCertificationPermission();

        return IsDocumentChangeForbidden(certPerm);
    }

    private static bool IsDocumentChangeForbidden(CertificationPermission? certificationPermission)
    {
        if (certificationPermission is null)
            return false;

        return certificationPermission == CertificationPermission.NO_CHANGE_PERMITTED;
    }

    public CertificationPermission? GetCertificationPermission()
    {
        int certificationLevel = GetCertificationLevel();

        if (certificationLevel > 0)
        {
            return GetCertificationPermissionByNumber(certificationLevel);
        }

        return null;
    }

    public static CertificationPermission? GetCertificationPermissionByNumber(int certificationLevel)
    {
        return certificationLevel switch
        {
            1 => CertificationPermission.NO_CHANGE_PERMITTED,
            2 => CertificationPermission.MINIMAL_CHANGES_PERMITTED,
            3 => CertificationPermission.CHANGES_PERMITTED,
            _ => throw new InvalidDataException($"Not supported /DocMDP code value : {certificationLevel}"),
        };
    }

    public int GetCertificationLevel()
    {
        PdfDictionary dic = _pdfReaderWrapper.RootCatalog.GetAsDictionary(PdfName.Perms);

        if (dic is null)
        {
            return (int)PdfSignatureAppearance.NOT_CERTIFIED;
        }

        dic = dic.GetAsDictionary(PdfName.DocMDP);

        if (dic is null)
        {
            return (int)PdfSignatureAppearance.NOT_CERTIFIED;
        }

        PdfArray arr = dic.GetAsArray(PdfName.Reference);

        if (arr is null || arr.Size() == 0)
        {
            return (int)PdfSignatureAppearance.NOT_CERTIFIED;
        }

        dic = arr.GetAsDictionary(0);

        if (dic is null)
        {
            return (int)PdfSignatureAppearance.NOT_CERTIFIED;
        }

        dic = dic.GetAsDictionary(PdfName.TransformParams);

        if (dic is null)
        {
            return (int)PdfSignatureAppearance.NOT_CERTIFIED;
        }

        PdfNumber p = dic.GetAsNumber(PdfName.P);

        if (p is null)
        {
            return (int)PdfSignatureAppearance.NOT_CERTIFIED;
        }

        return p.IntValue();
    }

    public enum CertificationPermission
    {
        NO_CHANGE_PERMITTED = 1,
        MINIMAL_CHANGES_PERMITTED = 2,
        CHANGES_PERMITTED = 3
    }

    public enum PdfSignatureAppearance
    {
        NOT_CERTIFIED = -1,
        CERTIFIED_ALL_CHANGES_ALLOWED = 0,
        CERTIFIED_NO_CHANGES_ALLOWED = 1,
        CERTIFIED_FORM_FILLING = 2,
        CERTIFIED_FORM_FILLING_AND_ANNOTATIONS = 3
    }

    private bool CanFillSignatureForm()
    {
        if (!_pdfReaderWrapper.IsOpenedWithFullPermission)
        {
            int permissions = _pdfReaderWrapper.Permissions;

            return IsAllowModifyAnnotations(permissions) || IsAllowFillIn(permissions);
        }

        return true;
    }

    private bool CanCreateSignatureField()
    {
        if (!_pdfReaderWrapper.IsOpenedWithFullPermission)
        {
            int permissions = _pdfReaderWrapper.Permissions;

            return IsAllowModifyContents(permissions) && IsAllowModifyAnnotations(permissions);
        }

        return true;
    }

    private static bool IsAllowModifyContents(int permissions)
    {
        return IsPermissionBitPresent(permissions, EncryptionConstants.ALLOW_MODIFY_CONTENTS);
    }

    private static bool IsAllowModifyAnnotations(int permissions)
    {
        return IsPermissionBitPresent(permissions, EncryptionConstants.ALLOW_MODIFY_ANNOTATIONS);
    }

    private static bool IsPermissionBitPresent(int permissions, int permissionBit)
    {
        return (permissionBit & permissions) > 0;
    }

    private static bool IsAllowFillIn(int permissions)
    {
        return IsPermissionBitPresent(permissions, EncryptionConstants.ALLOW_FILL_IN);
    }
}
