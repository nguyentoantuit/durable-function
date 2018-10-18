using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace VSSample.DurablePatterns
{

    public class SendAlertParameter
    {
        public string JobStatusUrl { get; set; }
    }
    public class FunctionMonitoring
    {
        private static HttpClient client = new HttpClient();

        [FunctionName("Monitor_SendAlert")]
        public static async Task Run(
            [OrchestrationTrigger] DurableOrchestrationContext ctx)
        {
            SendAlertParameter param = ctx.GetInput<SendAlertParameter>();
            DateTime expiryTime = DateTime.Now.AddMinutes(3);

            while (ctx.CurrentUtcDateTime < expiryTime)
            {
                var jobStatus = await ctx.CallActivityAsync<string>("GetJobStatus", param.JobStatusUrl);
                if (jobStatus == "Completed")
                {
                    // Perform action when condition met
                    await ctx.CallActivityAsync("SendAlert", "Test send alert message");
                    break;
                }

                // Orchestration will sleep until this time
                var nextCheck = ctx.CurrentUtcDateTime.AddSeconds(5);
                await ctx.CreateTimer(nextCheck, CancellationToken.None);
            }
        }

        [FunctionName("SendAlert")]
        public static async Task<string> SendAlert([ActivityTrigger] string message)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("AccessKey", "RV9aNyXY31IIAAsCmosAgE9zl");
            var options = new
            {
                Recipients = "+84915293595",
                Body = message,
                Originator = "Toan"
            };

            await client.PostAsJsonAsync("https://rest.messagebird.com/messages", options);

            return JsonConvert.SerializeObject(options);
        }

        [FunctionName("GetJobStatus")]
        public static async Task<string> GetJobStatus([ActivityTrigger] string url)
        {
            var jobStatus = await client.GetAsync(url);
            string result = await jobStatus.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(result))
            {
                var statusTest = await jobStatus.Content.ReadAsAsync<DurableOrchestrationStatus>(new[] { new JsonMediaTypeFormatter() });
                JObject status = JsonConvert.DeserializeObject<JObject>(result);
                if (status != null)
                {
                    return (string)status["runtimeStatus"];
                }
            }

            return null;
        }
    }
}
