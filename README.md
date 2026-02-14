# ESCLScan

**Simple console tool to scan multi-page documents from HP/printer with eSCL support, merge pages into one PDF, and (optionally) upload via FTP.**

Works well with most modern HP multifunction printers that support **eSCL**.

Current status: **minimal viable tool** – interactive prompt, no command-line arguments yet.

## Features

- Connects to scanner via **eSCL** protocol
- Scans one or multiple pages interactively
- Merges scanned pages into a single PDF
- Optional automatic upload via FTP
- Very simple prompt-based interface

## Requirements

- .NET 10
- (HP)-Scanner that supports **eSCL** scanning (tested on HP DeskJet 2630)
- Network connection to the printer
- (optional) FTP server for upload

## Usage

```bash
# Clone & build
git clone https://github.com/new-er/eSCL-scan.git
cd eSCL-scan
dotnet run
```

Follow the interactive prompts to scan pages, merge into PDF, and optionally upload via FTP.

## Contributing

Contributions are welcome! Feel free to submit issues or pull requests for improvements, bug fixes, or new features.
