# Azure AD Reader
This Azure Function App reads Azure AD Groups and fetches their members using the Graph API. This app supports Managed Identity and uses the MI to authenticate to Azure AD and Azure Data Lake. 

### GetGroups

Fetches the groups and members and writes the data into files in a specified Azure Data Lake (Gen 1) path

Usage: `GET /GetGroups/`

Request headers to the GET request:
* `ipGroups` : Semicolon (`;`) separated list of Azure AD Groups to retrieve
* `ipAdlsAccount` : Azure Data Lake Store Account Name. Give the full name (i.e. `xyz.azuredatalakestore.net`, not `xyz`)
* `ipAdlsPath` : Path in ADLS. Must include slashes in the beginning and at the end i.e. `/myfolder/`, not `myfolder`.

Note that the Function App Identity must have write permissions on the ADLS path. 

##### Output
Generates 4 files in the specified ADLS folder:
* `Group_xxxx.json`: Information about the requested AAD Groups
* `User_xxxx.json` : Regular users who are members
* `SPN_xxxx.json` : SPNs (includes managed identities) who are members
* `OtherMember_xxxx.json` : Other types of members such as Groups

To prevent collission of file names, each name is suffixed with a GUID (represented by `xxxx` above), so an actual file name could be `Member_28a634a4-8b48-4971-921e-b22e1c0c198a`.json for instance

### Configuration

1. Publish the project via Visual Studio
2. Modify the environment variable (app setting) called "AAD_TENANT" and set it to your Azure AD Tenant ID
3. An AAD admin needs to run the following Powershell script:

`$user = Get-AzureADServicePrincipal -Filter "ObjectId eq '<Func App Managed Identity ID>'"`

`$role = Get-AzureADDirectoryRole | Where-Object {$_.displayName -eq 'Directory Readers'}`

`Add-AzureADDirectoryRoleMember -ObjectId $role.ObjectId -RefObjectId $user.ObjectId`
