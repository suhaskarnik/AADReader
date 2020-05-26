using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.DataLake.Store;
using Microsoft.Extensions.Logging;

namespace AADReader.Helper
{
    public static class LakeWriter
    {
        public async static Task<Boolean> writeTextFile(string tenant, string adlsAccount, string path, string fileName, string content, ILogger log)
        {
            string accessToken = await AuthProvider.getAADToken(AuthProvider.ADLS_RESOURCE);
            var client = AdlsClient.CreateClient(adlsAccount, "Bearer " + accessToken);

            try
            {
                using (var stream = client.CreateFile(path + fileName, IfExists.Overwrite))
                {
                    byte[] textByteArray = Encoding.UTF8.GetBytes(content);
                    stream.Write(textByteArray, 0, textByteArray.Length);
                }
                return true;
            }
            catch (Exception e)
            {
                log.LogError($"\nError while writing file {adlsAccount + path + fileName}, error message from ADLS\n{e.Message}\nStack Trace:\n{e.StackTrace}\n\n");
                return false;
            }
        }

    }
}
