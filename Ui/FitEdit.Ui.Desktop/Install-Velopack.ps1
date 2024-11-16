$toolName = "vpk"
$toolInstalled = dotnet tool list -g | Select-String -Pattern $toolName

if ($toolInstalled) {
    echo "$toolName is already installed."
    #dotnet tool update -g $toolName
} else {
    echo "Installing $toolName..."
    dotnet tool install -g $toolName
}
