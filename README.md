# PdfSignabilityCheckerTool

A .NET 9 library for checking if a PDF file is signable, with support for encrypted PDFs and permission checks. Built using C# 13 and iText 7.
The purpose of this tool is to determine if a given PDF can be digitally signed with DSS (Digital Signature Service). It uses the same checking functionality as DSS to assess signability.
However, it does not support encrypted files when certain modifications are restricted, even if you possess the encryption key.

## Features

- Detects if a PDF is signable (including encrypted PDFs)
  - Checks if the PDF file can be opened and processed
  - Checks for permissions required to add signature fields
    - The PDF must not be encrypted or must allow signing (the ALLOW_MODIFY_CONTENTS and ALLOW_MODIFY_ANNOTATIONS permissions should be enabled without decrypting)
  - If any error occurs during processing, it will output an error message to the standard error stream

## Usage

To use the PdfSignabilityCheckerTool in Powershell, pipe the PDF file to the standard input:

```powershell
Get-Content -Path "C:\\Temp\\doc.pdf" -AsByteStream -Raw | .\PdfSignabilityCheckerTool.exe
```

The application will output the result to the standard output (with text 'true' or 'false'), indicating whether the PDF is signable or not.

Returns exit codes:
- `0`: the PDF is signable,
- `1`: the PDF is not signable, because modification of the content and/or creation of interactive fields, including signature fields, is disabled in the PDF.
- `99`: the PDF is not signable, an error occurred while processing the PDF.

## Requirements

- .NET 9
- iText 7 (iText.Kernel.Pdf)

## Development

- To publish to a single file executable, without containing the .Net Runtime, use the following command:
  ```powershell
  dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false
  ```

## License

This project is licensed under the AGPLv3 License.
