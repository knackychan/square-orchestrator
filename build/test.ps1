param(
    [ValidateSet('Deterministic','Architecture','UI','Prototype','Windows','Provider','Recovery','EndToEnd')]
    [string]$Category = 'Deterministic'
)
. (Join-Path $PSScriptRoot 'common.ps1')
$root = Get-SquareRepositoryRoot
Push-Location $root
try {
    switch ($Category) {
        'Deterministic' {
            Assert-SquareDotNetSdk
            & dotnet run --project tests/Domain.Tests/Domain.Tests.csproj
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
            & dotnet run --project tests/Contract.Tests/Contract.Tests.csproj
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
            & dotnet run --project tests/Persistence.Tests/Persistence.Tests.csproj
            exit $LASTEXITCODE
        }
        'Architecture' {
            Assert-SquareDotNetSdk
            Assert-SquareNodeVersion
            & dotnet run --project tests/Architecture.Tests/Architecture.Tests.csproj
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
            & node build/verify-repository.mjs
            exit $LASTEXITCODE
        }
        'UI' {
            Assert-SquareNodeVersion
            Invoke-SquarePnpm install --frozen-lockfile
            Invoke-SquarePnpm run check
            exit 0
        }
        'Prototype' {
            Assert-SquareDotNetSdk
            Assert-SquareNodeVersion
            & dotnet run --project prototypes/PipeProof/Square.PipeProof.Tests/Square.PipeProof.Tests.csproj
            if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
            & node --test prototypes/PipeProof/node-client/test/*.test.mjs
            exit $LASTEXITCODE
        }
        default {
            throw "Category '$Category' is reserved but not implemented in this bootstrap. See tests/test-suites.json."
        }
    }
} finally {
    Pop-Location
}
