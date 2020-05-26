# Overview
This Azure Function App reads Azure AD Groups and fetches their members

### GetGroups

Fetches the groups and members and writes this data into files in a Azure Data Lake (Gen 1) specified

Usage: `GET|POST /GetGroups/ ` 


### Installation Steps

1. Publish the project via Visual Studio
2. Modify the environment variable (app setting) called "AAD_TENANT" and set it to your Azure AD Tenant ID
3. An AAD admin needs to run the following Powershell script:

`$user = Get-AzureADServicePrincipal -Filter "ObjectId eq '<Func App Managed Identity ID>'"`

`$role = Get-AzureADDirectoryRole | Where-Object {$_.displayName -eq 'Directory Readers'}`

`Add-AzureADDirectoryRoleMember -ObjectId $role.ObjectId -RefObjectId $user.ObjectId`
