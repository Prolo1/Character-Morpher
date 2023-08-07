#This script gets the Version number from the Assembly (i.e. .DLL, .exe, ...), 
#adds it to the compressed folder name, then calls 
#another script of choice with unlimited arguments😎

$assemblyPath = "$($args[0])"
$userCommand = "$($args[1])"

$version = (Get-Command ${assemblyPath}).FileVersionInfo.FileVersionRaw

if ($($args[2]) -ilike '*.zip') {
    $args[2] = ($($args[2]) -split ".zip")[0] + "-V" + $version + ".zip"
}
#I dont have to do this but why not ¯\_(ツ)_/¯
elseif ($($args[2]) -ilike '*.7z') {
    $args[2] = ($($args[2]) -split ".7z")[0] + "-V" + $version + ".7z"
}

$userArgs = $args[2..($args.Count - 1)]

& ${userCommand} $userArgs | ForEach-Object { "$PSItem " }

break #The script should end here