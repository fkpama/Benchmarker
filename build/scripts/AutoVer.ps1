param(
    [Parameter(Position=1)][string]$TagPrefix = 'pre-prelease/v',
    [Parameter()][string]$PreRelease = 'preview'
)

$ErrorActionPreference='Stop'

[string]$current = &dotnet minver -t $TagPrefix -i -p $PreRelease --auto-increment patch
$next = &dotnet minver -t $TagPrefix -p $PreRelease --auto-increment patch

$idx=$current.LastIndexOf('.')
$height = $current.Substring($idx + 1)
if ( -not $height -match '^[\d\.]+$' ) {
    $height = 0
    $next = "$next.0"
}
Write-Output "Computed: $current => $next"
if ($current -ne $next)
{
    $idx=$current.LastIndexOf('.')
    $height = $current.Substring($idx + 1)
    if ( $height -match '^[\d\.]+$' ) {
        [string]$base=$current.Substring(0, $idx)
        $next="$base.$(1 + [int]$height)"
    }
    
    Write-Output "NEXT: $current => $next"
    Write-Output "##vso[task.setvariable variable=PackageVersion;]$next"
}
else
{
    Write-Output "Nothing to do"
}