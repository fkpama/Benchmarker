param(
    [Parameter(Position=1)][string]$TagPrefix = 'pre-release/v',
    [Parameter()][string]$PreRelease = 'preview',
    [Parameter()][switch]$NoToolRestore,
    [Parameter()][switch]$NoTagFetch
)

$ErrorActionPreference='Stop'

if ( -not $NoToolRestore.IsPresent ) {
    dotnet tool restore
}

if ( -not $NoTagFetch.IsPresent ) {
    Write-Host "##[info]Deleting tags"
    git tag -l | foreach { &git tag -d $_ }
    Write-Host "##[command]git fetch origin 'refs/tags/*:refs/tags/*'"
    git fetch origin 'refs/tags/*:refs/tags/*'
}

[string]$current = &dotnet minver -t $TagPrefix -i -p $PreRelease --auto-increment patch
$next = &dotnet minver -t $TagPrefix -p $PreRelease --auto-increment patch

Write-Output "Computed: $current => $next"
if ($current -ne $next)
{
    $idx=$current.LastIndexOf('.')
    $height = $current.Substring($idx + 1)
    if ( $height -match '^[\d\.]+$' ) {
        [string]$base = $current.Substring(0, $idx)
        $next="$base.$(1+ [int]$height)"
    }
    Write-Output "NEXT: $current => $next"
    Write-Output "##vso[task.setvariable variable=PackageVersion;]$next"
    Write-Output "##vso[task.setvariable variable=PackageGitTag;]$TagPrefix$next"
}
else
{
    Write-Output "Nothing to do"
}