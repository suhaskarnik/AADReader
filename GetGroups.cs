using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Services.AppAuthentication;
using Graph = Microsoft.Graph;
using System.Collections.Generic;

namespace AADReader
{
    public static class GetGroups
    {
        [FunctionName("GetGroups")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            List<String> groupNames = new List<string> { "SQLServer_Admins", "ADLS_Writer", "SS_Read_All", "SS_Write_All" };

            List<AADGroup> aadGroups = new List<AADGroup>();

            log.LogInformation("C# HTTP trigger function processed a request.");

            var client = GetGraphApiClient(log).Result;

            log.LogInformation($"Client: {client.ToString()}");




            foreach (string groupName in groupNames)
            {
                aadGroups.AddRange(getGroup(client, groupName));
            }



            string responseMessage = "Hello\n";
            foreach (AADGroup group in aadGroups)
            {
                responseMessage += $"\n\n{group.Id}\t{group.DisplayName}\n";
                foreach (Graph.DirectoryObject groupMember in group.members)
                {
                    responseMessage += $"\t\t{groupMember.Id}\t";
                }
            }

            


            return new OkObjectResult(responseMessage);
        }
        
        private static List<AADGroup> getGroup(Graph.GraphServiceClient client, String groupName)
        {
            List<AADGroup> aadGroups = new List<AADGroup>();            
            Graph.IGraphServiceGroupsCollectionPage groups = client.Groups
                                .Request()
                                .Filter($"DisplayName eq '{groupName}'")
                                .GetAsync().Result;

            var groupIterator = Graph.PageIterator<Graph.Group>.CreatePageIterator(client, groups, (g) =>
            {
                aadGroups.Add(new AADGroup(client, g));                
                return true;
            });
            
            
            groupIterator.IterateAsync();
            return aadGroups;
        }

        


        private static async Task<Graph.GraphServiceClient> GetGraphApiClient(ILogger log)
        {

            var azureServiceTokenProvider = new AzureServiceTokenProvider();

            string accessToken = await azureServiceTokenProvider
                .GetAccessTokenAsync("https://graph.microsoft.com/", Environment.GetEnvironmentVariable("AAD_TENANT"));
            log.LogInformation($"Access Token: {accessToken}");
                       
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
            public string Id;
            public string DisplayName;
            public List<Graph.DirectoryObject> members;

            public AADGroup(Graph.GraphServiceClient client, Graph.Group g)
            {
                Id = g.Id;
                DisplayName = g.DisplayName;
                Graph
                g.Members

                
                var memberIterator = Graph.PageIterator<Graph.DirectoryObject>.CreatePageIterator(client, g.Members, (gm) =>
                {
                    
                    members.Add(gm);
                    
                    return true;
                }
                
                );
                memberIterator.IterateAsync();
            }
        }

    }
}
