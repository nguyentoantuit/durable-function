using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VSSample.DurablePatterns
{
    public class ApprovalRequest
    {
        public bool IsApproved { get; set; }
    }

    public static class FunctionHumanInteracion
    {
        [FunctionName("Human_RequestApproval")]
        public static async Task<string> Run([OrchestrationTrigger] DurableOrchestrationContext ctx)
        {
            string approvalObj = "Approval remove durable function";
            await ctx.CallActivityAsync("RequestApproval", approvalObj);
            using (var timeoutCts = new CancellationTokenSource())
            {
                DateTime dueTime = ctx.CurrentUtcDateTime.AddMinutes(2);
                Task durableTimeout = ctx.CreateTimer(dueTime, timeoutCts.Token);

                Task<ApprovalRequest> approvalEvent = ctx.WaitForExternalEvent<ApprovalRequest>("ApprovalEvent");
                if (approvalEvent == await Task.WhenAny(approvalEvent, durableTimeout))
                {
                    timeoutCts.Cancel();
                    return await ctx.CallActivityAsync<string>("ProcessApproval", approvalEvent.Result);
                }
                else
                {
                    return await ctx.CallActivityAsync<string>("Escalate", "thanh.tran@devinition.com.vn");
                }
            }
        }

        [FunctionName("RequestApproval")]
        public static string RequestApproval([ActivityTrigger] string name)
        {
            return $"Request approval {name} is created";
        }

        [FunctionName("ProcessApproval")]
        public static string ProcessApproval([ActivityTrigger] ApprovalRequest approvalRequest)
        {
            if (approvalRequest.IsApproved)
            {
                return $"Request is approved";
            }
            else
            {
                return $"Request is rejected";
            }
        }

        [FunctionName("Escalate")]
        public static string Escalate([ActivityTrigger] string name)
        {
            return $"I want to escalate {name} because he didn't approve my request";
        }

        [FunctionName("RequestApprove")]
        public static async Task RequestApprove([HttpTrigger(AuthorizationLevel.Function, methods: "post", Route = "approval/{instanceId}")] HttpRequestMessage req,
            [OrchestrationClient] DurableOrchestrationClient client,
            string instanceId)
        {
            dynamic eventData = await req.Content.ReadAsAsync<object>();
            await client.RaiseEventAsync(instanceId, "ApprovalEvent", eventData);
        }
    }
}
