# PdfSignabilityCheckerTool

A small .NET 9 console utility that checks whether a PDF can be digitally signed.  
It performs the same signability checks used by DSS (Digital Signature Service), including permission checks and handling of encrypted PDFs when possible. The tool reads a raw PDF byte stream from standard input and writes a minimal result to standard output.

Supported runtime and libraries:

- .NET 9
- C# 13
- iText for PDF parsing

Quick summary

- Input: PDF bytes on stdin
- Output: `true` or `false` on stdout
- Errors, diagnostics and reasons are written to stderr
- Exit codes indicate specific outcomes (see below)

## Features

- Detects if a PDF is signable (including encrypted PDFs)
  - Checks if the PDF file can be opened and processed
  - Checks for permissions required to add signature fields in certain dictionaries
    - /DocMDP
    - /FieldMDP
    - /Lock
- If any error occurs during processing, it will output an error message to the standard error stream

## Behavior

- The tool attempts to open and inspect the PDF structure without mutating the file.
- It verifies PDF permissions bits required to create or modify signature fields and inspects DocMDP / FieldMDP / Lock dictionaries that can restrict adding new signatures.
- For encrypted PDFs, signability is determined by whether the document can be opened with enough permissions and whether the PDF permissions allow creating signature fields. If the PDF is encrypted with a certificate and cannot be opened, the tool reports that condition (see exit codes).

## Usage

To use the PdfSignabilityCheckerTool in Powershell, pipe the PDF file to the standard input:

```powershell
Get-Content -Path "C:\Temp\doc.pdf" -AsByteStream -Raw | .\PdfSignabilityCheckerTool.exe
```

## Command-line parameters

- `--unencrypted-is-signable`  
  Treat unencrypted documents (or documents opened with full permissions) as signable without further checks.
- `-sigfieldname <name>`  
  When present, used to evaluate FieldMDP / lock dictionaries that reference specific field names.

Returns exit codes:

- `0` - PDF is signable,
- `1` - PDF is not signable (permissions or restrictions prevent signature creation)
- `2` - PDF is password-protected and cannot be opened
- `66` - PDF is encrypted with a certificate and cannot be opened without the private key
- `99` - Processing error or unexpected exception (see stderr for the error)

## Requirements

- .NET 9

## Building and Publishing

- Build:

```powershell
dotnet build -c Release
```

- To publish to a single file executable, without containing the .Net Runtime, use the following command:

```powershell
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true --self-contained false
```

## License

This project is licensed under the AGPLv3 License.
