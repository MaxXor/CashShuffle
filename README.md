# CashShuffle

Implementation of the [CashShuffle](https://cashshuffle.com) protocol for privacy-enhanced transactions on Bitcoin Cash, based on [CoinShuffle](https://crypsys.mmci.uni-saarland.de/projects/CoinShuffle/coinshuffle.pdf). Compatible with the Electron Cash [CashShuffle plugin](https://github.com/cashshuffle/cashshuffle-electron-cash-plugin).

## Supported runtimes

- .NET Core 1.0+
- .NET Framework 4.6+

## Compile

Compile with Visual Studio or switch to the `src` directory and run:

`dotnet build -f netcoreapp1.0`

## Usage

Certificates must be PFX-encoded. You can use OpenSSL to convert from PEM or other formats.

```
Usage: dotnet run -- [OPTIONS]
  -c, --certificate=VALUE    Path to certificate file in PFX format.
  -p, --port=VALUE           Server port (default 8080).
  -s, --size=VALUE           Pool size (default 5).
  -h, --help                 Show help information.
```

When starting the server pass the certificate file as command line argument.

`dotnet run -- -c /etc/certs/cashshuffle.pfx`
