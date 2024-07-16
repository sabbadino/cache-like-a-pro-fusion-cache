1..100 | ForEach-Object -Parallel { 
$gdb  = Get-Date
$Response = Invoke-WebRequest -URI https://fusion-cache-api-west-1.azurewebsites.net/InMemoryCache/cache-stampede-in-memory?sleepInSeconds=5
$gd  = Get-Date
$el = $gd -$gdb # duration 
#write-host $el.TotalSeconds       $_ $Response
write-host $_ $Response
}  -ThrottleLimit 50