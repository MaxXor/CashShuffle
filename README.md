# CashShuffle

Implementation of the [CashShuffle](https://cashshuffle.com) protocol for privacy-enhanced transactions on Bitcoin Cash, based on [CoinShuffle](https://crypsys.mmci.uni-saarland.de/projects/CoinShuffle/coinshuffle.pdf).

## Supported runtimes

- .NET Core 2.1+

## Compile

Switch to the `src` directory and run:

`dotnet build`

## Usage

Certificates must be PFX-encoded. You can use OpenSSL to convert from PEM or other formats.

```
Usage: dotnet run -- [OPTIONS]
  -c, --certificate=VALUE    Path to certificate file in PFX format.
  -s, --size=VALUE           Pool size (default 5).
  -h, --help                 Show help information.
```

When starting the server pass the certificate file as command line argument.

`dotnet run -- -c /etc/certs/cashshuffle.pfx`
