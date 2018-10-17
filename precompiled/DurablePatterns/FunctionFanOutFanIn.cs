using Microsoft.Azure.WebJobs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VSSample.DurablePatterns
{
    public static class FunctionFanOutFanIn
    {
        [FunctionName("FanOut_FanIn_CalculateChars")]
        public static async Task<string> Run(
            [OrchestrationTrigger] DurableOrchestrationContext ctx)
        {
            var parallelTasks = new List<Task<int>>();

            // get a list of N work items to process in parallel
            string[] workBatch = await ctx.CallActivityAsync<string[]>("GetProcessStrings", null);
            for (int i = 0; i < workBatch.Length; i++)
            {
                Task<int> task = ctx.CallActivityAsync<int>("CalculateCharactorN", workBatch[i]);
                parallelTasks.Add(task);
            }

            await Task.WhenAll(parallelTasks);

            // aggregate all N outputs and send result to F3
            double sum = parallelTasks.Sum(t => t.Result);
            return await ctx.CallActivityAsync<string>("GetResultString", sum);
        }

        [FunctionName("GetProcessStrings")]
        public static string[] GetProcessStrings([ActivityTrigger] string parameter)
        {
            // Get data from other service
            var wholevalue = @"
            At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint occaecati cupiditate non provident, similique sunt in culpa qui officia deserunt mollitia animi, id est laborum et dolorum fuga. Et harum quidem rerum facilis est et expedita distinctio. Nam libero tempore, cum soluta nobis est eligendi optio cumque nihil impedit quo minus id quod maxime placeat facere possimus, omnis voluptas assumenda est, omnis dolor repellendus. Temporibus autem quibusdam et aut officiis debitis aut rerum necessitatibus saepe eveniet ut et voluptates repudiandae sint et molestiae non recusandae. Itaque earum rerum hic tenetur a sapiente delectus, ut aut reiciendis voluptatibus maiores alias consequatur aut perferendis doloribus asperiores repellat.
            ";

            return wholevalue.Split(',');
        }

        [FunctionName("CalculateCharactorN")]
        public static int CalculateCharactorN([ActivityTrigger] string value)
        {
            int sum = 0;
            for (int index = 0; index < value.Length; index++)
            {
                if (value[index] == 'N' || value[index] == 'n')
                {
                    sum++;
                }
            }

            return sum;
        }

        [FunctionName("GetResultString")]
        public static string GetResultString([ActivityTrigger] double sum)
        {
            return $"Total chars 'n/N' is {sum}";
        }
    }
}
