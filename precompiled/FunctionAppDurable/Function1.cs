using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FunctionAppDurable
{
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient client, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var queryValue = req.GetQueryNameValuePairs();
            string name = "";
            if (queryValue.Any(x => x.Key == "name"))
            {
                name = queryValue.First(x => x.Key == "name").Value;
            }

            var instanceId = await client.StartNewAsync("Hello_Durable", name);

            return client.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("Hello_Durable")]
        public static async Task<string> HelloDurable([OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var name = context.GetInput<string>();
            return await context.CallActivityAsync<string>("Hello", name);
        }


        [FunctionName("Hello")]
        public static string Hello([ActivityTrigger] string name)
        {
            return $"Hello {name}";
        }
    }
}
