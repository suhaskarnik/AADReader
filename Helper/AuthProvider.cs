using System;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;
using Graph = Microsoft.Graph;

namespace AADReader.Helper
{
    public static class AuthProvider
    {
        public const string GRAPH_RESOURCE = "https://graph.microsoft.com/";
        public const string ADLS_RESOURCE = "https://datalake.azure.net/";

        public static async Task<Graph.GraphServiceClient> getGraphApiClient(ILogger log)
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

        public static async Task<String> getAADToken(string resource)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            return await azureServiceTokenProvider.GetAccessTokenAsync(resource, Environment.GetEnvironmentVariable("AAD_TENANT"));
        }
    }
}
