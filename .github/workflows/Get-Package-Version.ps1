$debugMode = $env:SYSTEM_DEBUG

# This function finds the version of a given package that is rerference from a given packages.config file
function Get-Package-Version ([string] $PackageFilePath, [string] $PackageName) {
    if($debugMode -eq $true) {
        Write-Host "Get-Package-Version Searching package file '$PackageFilePath' for package '$PackageName'"
    }

    $packageNode = (Select-Xml -Path $PackageFilePath -XPath "//package[@id='$PackageName']" | Select-Object -ExpandProperty "node")

    if($packageNode -eq $null) {
        if($debugMode -eq $true) {
            Write-Host "Get-Package-Version Package reference not found"
        }

        return $null
    }

    $result = $packageNode | Select-Object -ExpandProperty "version"

    if($debugMode -eq $true) {
        Write-Host "Get-Package-Version Found version $result"
    }
    return $result
}
