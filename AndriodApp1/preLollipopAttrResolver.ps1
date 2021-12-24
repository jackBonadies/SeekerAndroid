#arg0 is original file name
#arg1 is ex. "?attr/mainPurple" to replace
#other args are to resolve ?attr to
$fname=$args[0]
$replace1=$args[1]

for (($i = 2); $i -lt $args.Length; $i++)
{
    $c = (Get-Content $fname) 
    $replaceWith = "#" + $args[$i]
    $c = $c -replace [regex]::escape($replace1), "$replaceWith"
    
    


    $newF = $fname.SubString(0, $fname.Length-4)
    $newF = $newF + '_' + $args[$i] + ".xml"

    [IO.File]::WriteAllText($newF, $c)
    write-host $newF
}

#write-host $fname
#$c = (Get-Content $fname) 
#$c = $c -replace 'attr','test'
#
#[IO.File]::WriteAllText($fname, ($c -join "`r`n"))
 