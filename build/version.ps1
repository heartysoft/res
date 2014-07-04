
Function Get-GitCommitHashAndTimestamp
{
    $lastCommitLog = Exec { git log --max-count=1 --pretty=format:%H@%aD HEAD } "Cannot execute git log. Ensure that the current directory is a git repository and that git is available on PATH."
    return $lastCommitLog
}

Function Get-GitCommitHash
{
    $lastCommitLog = Exec { git log --max-count=1 --pretty=format:%H HEAD } "Cannot execute git log. Ensure that the current directory is a git repository and that git is available on PATH."
    return $lastCommitLog
}

Function Get-GitTimestamp
{
    $lastCommitLog = Exec { git log --max-count=1 --pretty=format:%aD HEAD } "Cannot execute git log. Ensure that the current directory is a git repository and that git is available on PATH."
    return $lastCommitLog
}

Function Get-GitBranchOrTag
{
    $revParse = Exec { git rev-parse --abbrev-ref HEAD } "Cannot execute git rev-parse. Ensure that the current directory is a git repository and that git is available on PATH."
    if ($revParse -ne "HEAD") {
        return $revParse
    }

    $describeTags = Exec { git describe --tags } "Cannot execute git describe. Ensure that the current directory is a git repository and that git is available on PATH."
    return $describeTags
}

Function Patch-AssemblyInfo {
    Param(
        [Parameter(Mandatory=$true)]
        [string]$assemblyInfoFilePath,
        [Parameter(Mandatory=$true)]
        [string]$version,
        [Parameter(Mandatory=$true)]
        [string]$fileVersion,
        [Parameter(Mandatory=$true)]
        [string]$branch,
        [Parameter(Mandatory=$true)]
        [string]$commitHashAndTimestamp,
        #[Parameter(Mandatory=$true)]
        #[string]$productName,
        [Parameter(Mandatory=$true)]
        [string]$companyName,
        [Parameter()][string]$copyright
    )
    Process {
        $newAssemblyVersion = 'AssemblyVersion("' + $version + '")'
        $newAssemblyFileVersion = 'AssemblyFileVersion("' + $fileVersion + '")'
        $newAssemblyVersionInformational = 'AssemblyInformationalVersion("' + $version + '.' + $branch + '@' + $commitHashAndTimestamp + '")'
        $newAssemblyProductName = 'AssemblyProduct("' + $productName + '")'
        $newAssemblyCopyright = 'AssemblyCopyright("'+ $copyright + '")'
        $newAssemblyCompany = 'AssemblyCompany("' + $companyName + '")'

        $assemblyVersionPattern = 'AssemblyVersion\(".*"\)'
        $assemblyFileVersionPattern = 'AssemblyFileVersion\(".*"\)'
        $assemblyVersionInformationalPattern = 'AssemblyInformationalVersion\(".*"\)'
        #$assemblyProductNamePattern = 'AssemblyProduct\(".*"\)'
        $assemblyCopyrightPattern = 'AssemblyCopyright\(".*"\)'
        $assemblyCompanyPattern = 'AssemblyCompany\(".*"\)'

        $edited = (Get-Content $assemblyInfoFilePath) | ForEach-Object {
            % {$_ -replace "\/\*+.*\*+\/", "" } |
            % {$_ -replace "\/\/+.*$", "" } |
            % {$_ -replace "\/\*+.*$", "" } |
            % {$_ -replace "^.*\*+\/\b*$", "" } |
            % {$_ -replace $assemblyVersionPattern, $newAssemblyVersion } |
            % {$_ -replace $assemblyFileVersionPattern, $newAssemblyFileVersion } |
            % {$_ -replace $assemblyVersionInformationalPattern, $newAssemblyVersionInformational } |
            #% {$_ -replace $assemblyProductNamePattern, $newAssemblyProductName } |
            % {$_ -replace $assemblyCopyrightPattern, $newAssemblyCopyright } |
            % {$_ -replace $assemblyCompanyPattern, $newAssemblyCompany }
        }

        if (!(($edited -match $assemblyVersionInformationalPattern) -ne "")) {
            $edited += "[assembly: $newAssemblyVersionInformational]"
        }

        Set-Content -Path $assemblyInfoFilePath -Value $edited
    }
}