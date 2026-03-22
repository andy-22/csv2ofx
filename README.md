# csv2ofx

Convert brokerage CSV exports into OFX investment statements.

The current CLI is focused on Fidelity brokerage CSV files and emits OFX 2.1.1 style investment transactions suitable for import into personal finance tools that accept investment OFX.

## Current Support

- Input provider: `fidelity`
- Input format: CSV
- Output format: OFX investment statement
- Current parser type: brokerage transactions

## What It Already Handles

- Fidelity CSV files with blank rows before the real header
- Fidelity CSV files with disclaimer/footer text after the transaction section
- Auto-detection of the actual Fidelity header row
- Automatic skipping of trailing footer/disclaimer sections after blank lines
- Duplicate-safe auto-generated output file names when `--ofx` is not supplied
- Stable generated `FITID` values from row content
- Optional start-date filtering
- Security IDs as either mapped `CUSIP` values or raw `ticker` values
- Embedded security list in the generated OFX
- Common Fidelity action normalization:
  - buy / purchase / reinvestment
  - sell
  - dividend / interest / return of capital
  - transfers
  - fee / tax-style expenses
  - stock splits / reverse splits

## Requirements

- .NET 8 SDK

## Build

```bash
dotnet build CsvToOfx.sln
```

## Run

You can run the CLI directly from the repo:

```bash
dotnet run --project src/CsvToOfx.Cli -- --csv /path/to/fidelity.csv --acct-id 123456789
```

That will create an OFX file next to the CSV using the same base name, for example:

```text
/path/to/fidelity.ofx
```

If that file already exists, the program will create:

```text
/path/to/fidelity_1.ofx
/path/to/fidelity_2.ofx
...
```

## CLI Options

The program currently supports both `--key=value` and `--key value`.

### Required

- `--csv`
  - Path to the input CSV file
- `--acct-id`
  - Account ID to embed in the generated OFX

### Optional

- `--source`
  - Provider code
  - Default: `fidelity`
- `--ofx`
  - Output OFX path
  - If omitted, the tool auto-generates a non-conflicting `.ofx` path next to the CSV
- `--start-date`
  - Filters out rows before the given date
- `--security-id-type`
  - Controls how securities are identified in OFX
  - Supported values:
    - `cusip` (default)
    - `ticker`

## Examples

Basic conversion:

```bash
dotnet run --project src/CsvToOfx.Cli -- \
  --csv ~/Downloads/fidelity.csv \
  --acct-id Z12345678
```

Write to a specific OFX path:

```bash
dotnet run --project src/CsvToOfx.Cli -- \
  --csv ~/Downloads/fidelity.csv \
  --acct-id Z12345678 \
  --ofx ~/Downloads/fidelity-import.ofx
```

Filter to transactions on or after a date:

```bash
dotnet run --project src/CsvToOfx.Cli -- \
  --csv ~/Downloads/fidelity.csv \
  --acct-id Z12345678 \
  --start-date 2026-01-01
```

Use ticker symbols instead of mapped CUSIPs:

```bash
dotnet run --project src/CsvToOfx.Cli -- \
  --csv ~/Downloads/fidelity.csv \
  --acct-id Z12345678 \
  --security-id-type ticker
```

Using `--key=value` syntax:

```bash
dotnet run --project src/CsvToOfx.Cli -- \
  --csv=~/Downloads/fidelity.csv \
  --acct-id=Z12345678 \
  --start-date=3/1/26
```

## Accepted Date Formats

`--start-date` and Fidelity row dates currently accept these formats:

- `yyyy-MM-dd`
- `MM/dd/yyyy`
- `MM-dd-yyyy`
- `MM/dd/yy`
- `M/d/yy`
- `M/d/yyyy`
- `MM/d/yy`
- `M/dd/yy`
- `MM/d/yyyy`
- `M/dd/yyyy`

Examples:

- `2026-03-01`
- `03/01/2026`
- `3/1/26`

## Security ID Behavior

By default, the tool prefers `CUSIP` identifiers when a ticker can be mapped through the embedded security map.

If you pass:

```bash
--security-id-type ticker
```

the tool will keep securities as ticker-based IDs instead.

If a symbol already looks like a CUSIP, it is treated as a CUSIP automatically.

## OFX Output Behavior

The generated OFX currently includes:

- investment transaction list
- account ID from `--acct-id`
- generated `FITID` for each transaction
- security identifiers
- security list message set
- transaction memos when available
- normalized totals, units, and prices

Transaction categories currently written:

- `BUYSTOCK`
- `SELLSTOCK`
- `INCOME`
- `INVEXPENSE`
- `STOCKSPLIT`
- `INVBANKTRAN`

## Fidelity Notes

The Fidelity parser expects the standard transaction export columns and looks for a header containing:

- `Run Date`
- `Action`
- `Symbol`
- `Description`
- `Type`
- `Price`
- `Quantity`
- `Commission`
- `Fees`
- `Amount`

It now tolerates malformed exports where:

- the first few rows are blank
- blank rows appear between the header and footer/disclaimer area
- trailing disclaimer text appears after the real transaction block

## Defaults and Assumptions

- default source is `fidelity`
- default output currency is `USD`
- generated output always includes the security list
- output subaccount values are currently written as `CASH`
- unsupported or unknown actions currently fall back to cash-transfer behavior
- amount parsing is normalized to absolute values

## Exit Behavior

The CLI exits with an error if:

- `--csv` is missing
- `--acct-id` is missing
- `--source` is unknown

## Tests

Run tests with:

```bash
dotnet test tests/CsvToOfx.Core.Tests/CsvToOfx.Core.Tests.csproj
dotnet test tests/CsvToOfx.Parsers.Tests/CsvToOfx.Parsers.Tests.csproj
```

## Roadmap Gaps

Things clearly present in the codebase but not yet surfaced as broader product features:

- parser abstraction supports multiple providers, but only Fidelity is currently registered
- parser capability flags support non-CSV and non-brokerage formats, but they are not implemented here yet
- subaccount inference service exists, but the OFX writer currently emits `CASH` subaccounts
