
$manifestFilePath = $args[0]
$isIzzyDroid = $args[1] -eq "Release IzzySoft"
Write-Output $manifestFilePath
Write-Output $args[1]
if ($isIzzyDroid)
{
    Write-Output "IzzySoft"
    $alreadyContainsPermission = $false
    $arrayOfStrings = Get-Content $manifestFilePath
    foreach ($line in $arrayOfStrings)
    {
        if($line.Contains('<uses-permission android:name="android.permission.MANAGE_EXTERNAL_STORAGE"'))
        {
            $alreadyContainsPermission = $true
        }
    }
    if($alreadyContainsPermission)
    {
        exit 0
    }
    else
    {
        [Collections.Generic.List[String]]$lstOfStrings = $arrayOfStrings
        $lstOfStrings.Insert($lstOfStrings.Count - 3,"`t" + '<uses-permission android:name="android.permission.MANAGE_EXTERNAL_STORAGE" />')
        Set-Content -Path $manifestFilePath -Value $lstOfStrings
    }

}
else
{
    Write-Output "playstore"
    $alreadyContainsPermission = $false
    $arrayOfStrings = Get-Content $manifestFilePath
    $found = $false
    $index = 0
    foreach ($line in $arrayOfStrings)
    {
        if($line.Contains('<uses-permission android:name="android.permission.MANAGE_EXTERNAL_STORAGE"'))
        {
            $found = $true
            break
        }
        $index = $index + 1
    }
    if($found)
    {
        [Collections.Generic.List[String]]$lstOfStrings = $arrayOfStrings
        $lstOfStrings.RemoveAt($index)
        Set-Content -Path $manifestFilePath -Value $lstOfStrings
    }
}
    