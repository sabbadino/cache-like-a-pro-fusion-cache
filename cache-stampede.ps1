1..100 | ForEach-Object -Parallel { 
$gdb  = Get-Date
#$Response = Invoke-WebRequest -URI https://localhost:7286/SimpleCache/cache-stampede?sleepInSeconds=5
$Response = Invoke-WebRequest -URI https://localhost:7286/FusionCache/cache-stampede?sleepInSeconds=5
$gd  = Get-Date
$el = $gd -$gdb # duration 
#write-host $el.TotalSeconds       $_ $Response
write-host $_ $Response
}  -ThrottleLimit 50