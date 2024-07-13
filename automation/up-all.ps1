# az config set core.login_experience_v2=off
# az config set core.enable_broker_on_windows=false
az login --tenant 088e9b00-ffd0-458e-bfa1-acf4c596d3cb 
az account set --subscription "Azure DevTest Academy"
az deployment group what-if --resource-group "poc-fusion-cache" --template-file "up-webapps.bicep"
# az deployment group what-if --resource-group "poc-fusion-cache" --template-file "up-redis.bicep"
#az deployment group create --resource-group "poc-fusion-cache" --template-file "up-webapps.bicep"
#az deployment group create --resource-group "poc-fusion-cache" --template-file "up-redis.bicep"

