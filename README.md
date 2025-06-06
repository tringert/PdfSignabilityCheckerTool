# PdfSignabilityCheckerTool

A .NET 9 library for checking if a PDF file is signable, with support for encrypted PDFs and permission checks. Built using C# 13 and iText 7.
The purpose of this tool is to determine if a given PDF can be digitally signed with DSS (Digital Signature Service). It uses the same checking functionality as DSS to assess signability.
However, it does not support encrypted files when certain modifications are restricted, even if you possess the encryption key.

## Features

- Detects if a PDF is signable (including encrypted PDFs)
  - Checks if the PDF file can be opened
  - Checks for permissions required to add signature fields
    - The PDF must not be encrypted or must allow signing (the ALLOW_MODIFY_CONTENTS and ALLOW_MODIFY_ANNOTATIONS permissions should be enabled without decrypting)
  - Throws clear exceptions if signing is not allowed

## Usage

To use the PdfSignabilityCheckerTool in Powershell, pipe the PDF file to the standard input:

```powershell
Get-Content -Path "C:\\Temp\\doc.pdf" -AsByteStream -Raw | .\PdfSignabilityCheckerTool.exe
```

## Requirements

- .NET 9
- iText 7 (iText.Kernel.Pdf)

## Development

- To publish in the indicated Native AOT format, the C++ Desktop Development Tools must be installed in Visual Studio.

## License

This project is licensed under the AGPLv3 License.
