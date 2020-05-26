using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AADReader.Helper;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Graph = Microsoft.Graph;

namespace AADReader
{
    public static class GetGroups
    {
        [FunctionName("GetGroups")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string ipGroups = req.Query["aadGroups"];
            string ipAdlsAccount = req.Query["AdlsAccount"];
            string ipAdlsPath = req.Query["AdlsPath"];

            List<String> groupNames = new List<String>(ipGroups.Split(";"));

            log.LogInformation("C# HTTP trigger function processed a request.");

            var client = GetGraphApiClient(log).Result;
            if (client != null) { log.LogInformation($"Received client"); }

            List<AADGroup> aadGroups = new List<AADGroup>();
            List<AADGroupMember> otherMembers = new List<AADGroupMember>();
            List<AADUser> users = new List<AADUser>();
            List<AADServicePrincipal> servicePrincipals = new List<AADServicePrincipal>();
            Dictionary<string, string> fileNames = new Dictionary<string, string>();


            foreach (string groupName in groupNames)
            {
                Graph.IGraphServiceGroupsCollectionPage groups = await
                    client.Groups.Request()
                    .Filter($"DisplayName eq '{groupName}'")
                    .Expand("Members")
                    .GetAsync();


                if (groups?.Count > 0)
                {
                    foreach (Graph.Group group in groups)
                    {
                        AADGroup aadGroup = new AADGroup(group.Id, group.DisplayName);
                        log.LogInformation($"Processing '{group.DisplayName}'");
                        if (group.Members?.Count > 0)
                        {

                            foreach (Graph.DirectoryObject dobj in group.Members)
                            {
                                switch (dobj)
                                {
                                    case Graph.User user:
                                        users.Add(new AADUser(group, user.Id, "User", user.DisplayName, user.UserPrincipalName));
                                        break;
                                    case Graph.ServicePrincipal spn:
                                        servicePrincipals.Add(new AADServicePrincipal(group, spn.Id, "SPN", spn.DisplayName, spn.AppId));
                                        break;
                                    default:
                                        otherMembers.Add(new AADGroupMember(group, dobj.Id, dobj.GetType().ToString()));
                                        break;
                                }

                            }

                        }
                        aadGroups.Add(aadGroup);
                    }
                }
            }


            string filename;
            bool success;


            filename = $"User_{Guid.NewGuid()}.json";
            success = await LakeWriter.writeTextFile(Environment.GetEnvironmentVariable("AAD_TENANT"), ipAdlsAccount,
                ipAdlsPath, filename, JsonConvert.SerializeObject(users), log);
            if (success) { fileNames.Add("User", filename); }

            filename = $"Group_{Guid.NewGuid()}.json";
            success = await LakeWriter.writeTextFile(Environment.GetEnvironmentVariable("AAD_TENANT"), ipAdlsAccount,
                ipAdlsPath, filename, JsonConvert.SerializeObject(aadGroups), log);
            if (success) { fileNames.Add("Group", filename); }

            filename = $"SPN_{Guid.NewGuid()}.json";
            success = await LakeWriter.writeTextFile(Environment.GetEnvironmentVariable("AAD_TENANT"), ipAdlsAccount,
                ipAdlsPath, filename, JsonConvert.SerializeObject(servicePrincipals), log);
            if (success) { fileNames.Add("SPN", filename); }

            filename = $"OtherMember_{Guid.NewGuid()}.json";
            success = await LakeWriter.writeTextFile(Environment.GetEnvironmentVariable("AAD_TENANT"), ipAdlsAccount,
                ipAdlsPath, filename, JsonConvert.SerializeObject(otherMembers), log);
            if (success) { fileNames.Add("OtherMember", filename); }

            log.LogInformation(fileNames.ToString());
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(fileNames), Encoding.UTF8, "application/json")
            };

        }




        private static async Task<Graph.GraphServiceClient> GetGraphApiClient(ILogger log)
        {

            var azureServiceTokenProvider = new AzureServiceTokenProvider();

            string accessToken = await azureServiceTokenProvider
                .GetAccessTokenAsync("https://graph.microsoft.com/", Environment.GetEnvironmentVariable("AAD_TENANT"));

            var graphServiceClient = new Graph.GraphServiceClient(
                Graph.GraphClientFactory.Create(
                    new Graph.DelegateAuthenticationProvider((requestMessage) =>
                    {
                        requestMessage
                    .Headers
                    .Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("bearer", accessToken);

                        return Task.CompletedTask;
                    }))
                );

            return graphServiceClient;
        }


        private class AADGroup
        {
            [JsonProperty]
            private string Id;

            [JsonProperty]
            private string DisplayName;

            public AADGroup(string id, string displayName)
            {
                Id = id;
                DisplayName = displayName;
            }

        }

        private class AADGroupMember
        {
            [JsonProperty]
            private string groupId;

            [JsonProperty]
            private string groupName;

            [JsonProperty]
            private string id;

            [JsonProperty]
            private string memberType;

            public AADGroupMember(Graph.Group group, string id, string memberType)
            {
                this.groupId = group.Id;
                this.groupName = group.DisplayName;
                this.id = id;
                this.memberType = memberType;
            }
        }

        private class AADUser : AADGroupMember
        {
            [JsonProperty]
            private string displayName;

            [JsonProperty]
            private string userPrincipalName;

            public AADUser(Graph.Group group, string id, string memberType, string displayName, string userPrincipalName) : base(group, id, "User")
            {
                this.displayName = displayName;
                this.userPrincipalName = userPrincipalName;
            }

        }

        private class AADServicePrincipal : AADGroupMember
        {
            [JsonProperty]
            private string displayName;

            [JsonProperty]
            private string applicationId;

            public AADServicePrincipal(Graph.Group group, string id, string memberType, string displayName, string applicationId) : base(group, id, "SPN")
            {
                this.displayName = displayName;
                this.applicationId = applicationId;
            }

        }
    }


}
