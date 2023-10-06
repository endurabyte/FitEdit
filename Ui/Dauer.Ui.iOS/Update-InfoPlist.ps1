param (
    [string]$versionPrefix,
    [string]$buildIncrement,
    [string]$plistPath
)

$path = Convert-Path $plistPath
[xml]$xml = Get-Content $path

$versionNode = $xml.SelectSingleNode("//key[. = 'CFBundleShortVersionString']/following-sibling::string[1]")
$buildNode = $xml.SelectSingleNode("//key[. = 'CFBundleVersion']/following-sibling::string[1]")

$versionNode.InnerText = $versionPrefix
$buildNode.InnerText = $buildIncrement

$xml.Save($path)
