# Build Tool Template

Includes a build pipeline and tooling for your project in .NET 6+ using [Bullseye](https://github.com/adamralph/bullseye), [SimpleExec](https://github.com/adamralph/simple-exec) and [System.CommandLine](https://github.com/dotnet/command-line-api).

Use this project as a starting point for your own build pipeline.

## Install template

```powershell
dotnet new build-tool
```

## Usage

### Targets

List built-in targets:

```powershell
./build.ps1 targets -t
```

Result:

```powershell
build
└─clean-build-output
clean-artifacts-output
clean-build-output
default
├─run-tests
│ └─build
│   └─clean-build-output
└─publish-artifacts
  └─pack
    └─clean-artifacts-output
pack
└─clean-artifacts-output
publish-artifacts
└─pack
  └─clean-artifacts-output
restore-tools
run-tests
└─build
  └─clean-build-output
  ```

### Help

```powershell
./build.ps1 --help
```

### Debug

Open `build.csproj` with you favorite IDE and debug as usual

> You have to change  `Working Directory` and `Program arguments` in the configuration to Debug

## Attribution

[construction helmet](https://thenounproject.com/icon/construction-helmet-2074586/) by Template from [Noun Project](https://thenounproject.com/browse/icons/term/construction-helmet/) (CC BY 3.0)