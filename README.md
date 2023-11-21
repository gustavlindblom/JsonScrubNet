# JsonScrubNet

Small utility tool to scrub a json file and replace values with random garbage. Useful to share a json file to show structure but not accidentally leak sensitive information.

## Quick start

```console
    dotnet run <input file> <output file>
```

Sample output:

```console
    dotnet run input.json output.json
    INFO: Scrubbing json values in ~/input.json
    INFO: Wrote 49803892 bytes to ~/output.json
```
