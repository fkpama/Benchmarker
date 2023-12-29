param(
    [Parameter(Mandatory)]
    $Vsix,

    [Parameter()]
    $PAT
)

$Script:DefaultPAT

function Script:runTfxCli
{
    param(
        [Parameter()]
        $Arguments,

        [Parameter()]
        $PAT
    )

    process
    {
        $args = @()
        &npx tfx-cli --no-prompt  @Arguments
    }
}

function Script:checkNpxCli
{
    &npx --version
    if ($LASTEXITCODE -ne 0)
    {
        throw "npx command not found"
    }
}

function Script:findAccessToken()
{
    $prev = $null
    for ($cur = $PWD; $cur -ne $prev; $cur = (Split-Path -Parent $cur))
    {
        $path = Join-Path $cur 'pat.txt'
        if (Test-Path $path)
        {

            $content = Get-Content -Raw $path
            if ($content) { return $content }
        }
    }

    throw "Unable to find Personal Access Token"
}

$ErrorActionPreference = 'Stop'

if (-not $PAT)
{
    $PAT = findAccessToken
}

if (-not $Manifest)
{
    throw "Not implemented: get manifest from .vsix"
}

checkNpxCli

$traceSet = Test-Path Env:\TFX_TRACE
if ($traceSet)
{
    Remove-Item Env:\TFX_TRACE
}
if (-not (Test-Path $Vsix))
{
    throw "File not found $Vsix"
}
$output = &npx tfx-cli extension show --output json --no-prompt --token $PAT --vsix $Vsix
$content = ($output | ConvertFrom-Json)

$publisher = $content.publisher.publisherId
$extensionId = $content.extensionId

$output = &npx tfx-cli extension publish --no-prompt --output json --token $PAT --vsix $Vsix
$content = ($output | ConvertFrom-Json)

if (-not $content.published)
{
    throw "Failed to publish vsix:`n$content"
}