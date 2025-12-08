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
        _pdfReaderWrapper = new(ms);
        _signatureFieldId = signatureFieldId ?? "";
    }

    internal bool IsSignable(bool treatUnencryptedAsSignable)
    {
        bool treatOpenedWithFullPermissionAsSignable = (!_pdfReaderWrapper.IsEncrypted || _pdfReaderWrapper.IsOpenedWithFullPermission) && treatUnencryptedAsSignable;

        if (treatOpenedWithFullPermissionAsSignable)
            return true;

        (bool isSignable, string reason) = NewWay();

        if (!isSignable)
        {
            Console.Error.WriteLine(reason);
            return false;
        }

        return true;
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
            Dictionary<PdfSignatureDictionary, List<PdfSignatureField>> sigDictionaries = SignatureExtractor.ExtractSigDictionaries(_pdfReaderWrapper);

            // FieldMDP
            foreach (PdfSignatureDictionary signatureDictionary in sigDictionaries.Keys)
            {
                SigFieldPermissions? fieldMDP = signatureDictionary.GetFieldMDP();

                if (fieldMDP != null && IsSignatureFieldCreationForbidden(fieldMDP, _signatureFieldId))
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
                        IsSignatureFieldCreationForbidden(lockDict, _signatureFieldId))
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
            return Helpers.GetCertificationPermissionByNumber(certificationLevel);
        }

        return null;
    }

    public int GetCertificationLevel()
    {
        if (_pdfReaderWrapper.PermsDictionary is null)
        {
            return (int)PdfSignatureAppearance.NOT_CERTIFIED;
        }

        if (_pdfReaderWrapper.DocMdpDictionary is null)
        {
            return (int)PdfSignatureAppearance.NOT_CERTIFIED;
        }

        if (_pdfReaderWrapper.ReferenceArray is null || _pdfReaderWrapper.ReferenceArray.Size() == 0)
        {
            return (int)PdfSignatureAppearance.NOT_CERTIFIED;
        }

        if (_pdfReaderWrapper.ReferenceDictionary is null)
        {
            return (int)PdfSignatureAppearance.NOT_CERTIFIED;
        }

        if (_pdfReaderWrapper.TransformParamsDictionary is null)
        {
            return (int)PdfSignatureAppearance.NOT_CERTIFIED;
        }

        PdfNumber? p = _pdfReaderWrapper.P;

        if (p is null)
        {
            return (int)PdfSignatureAppearance.NOT_CERTIFIED;
        }

        return p.IntValue();
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
