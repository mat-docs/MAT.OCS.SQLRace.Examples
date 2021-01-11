. $PSScriptRoot"\Get-Package-Version.ps1"

function Check-References([string] $RootDirectory, [string] $PackageName, [string[]] $VersionMustContain, [string[]] $VersionMustNotContain) {
    # Find all of the packages.config files
    $packageFiles = @(Get-ChildItem -Path $RootDirectory -Filter packages.config -Recurse | Select-Object -ExpandProperty "FullName")
    
    # The value we will return to indicate whether the references as valid or not
    $validationSuccessful = $true

    foreach($packageFile in $packageFiles) { 
        $version = Get-Package-Version -PackageFilePath $packageFile -PackageName $PackageName

        if($version -eq $null) {
            # The package in question wasn't referenced from this packages.config file
            continue
        }
    
        # The version must contain each of the strings in $VersionMustContain array
        foreach($validRegex in $VersionMustContain){
            if($version -notmatch $validRegex) {
                # This is referencing a version of a package that we aren't permitted to reference
                $validationSuccessful = $false

                Write-Host "[ERROR]: Package file '$packageFile' is referencing an invalid build of '$packageName' - $version must contain $validRegex" -ForegroundColor red
            }
        }

        # The version must not contain any of the strings in $VersionMustNotContain array
        foreach($invalidRegex in $VersionMustNotContain){
            if($version -match $invalidRegex) {
                # This is referencing a version of a package that we aren't permitted to reference
                $validationSuccessful = $false

                Write-Host "[ERROR]: Package file '$packageFile' is referencing an invalid build of '$packageName' - $version must not contain $invalidRegex" -ForegroundColor red
            }
        }
    }

    if (-Not $validationSuccessful)
    {
        Write-Error "Error(s) found in package version."
    }
}

