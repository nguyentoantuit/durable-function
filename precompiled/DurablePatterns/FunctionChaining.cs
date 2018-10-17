using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSSample.DurablePatterns
{
    public class Dossier
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
    }

    public class Email
    {
        public string Subject { get; set; }
        public string To { get; set; }
        public string From { get; set; }
        public string Body { get; set; }
    }

    public static class FunctionChaining
    {
        private static readonly List<Dossier> Dossiers = new List<Dossier> {
            new Dossier { Id = new Guid("{DF8417DE-A33B-40DC-ADBA-B2FAC5FCE61F}"), Address = "BigC, To Hien Thanh - Q.10 - HCM", Name = "SWG" },
            new Dossier { Id = new Guid("{61CF442F-870E-48F1-877C-66F59C801803}"), Address = "121 Su Van Hanh - Q.10 - HCM", Name = "OAW" },
            new Dossier { Id = new Guid("{C61A66B2-A152-4590-9617-618F03DBE3ED}"), Address = "127 Bui Dinh Tuy - Q.Binh Thanh - HCM", Name = "DISKAS" }
        };

        private class SendEmailParameter
        {
            public Guid DossierId { get; set; }
        }

        [FunctionName("Chaining_SendEmail")]
        public static async Task<Email> Run(
            [OrchestrationTrigger] DurableOrchestrationContextBase context)
        {
            var param = context.GetInput<SendEmailParameter>();
            var dossier = await context.CallActivityAsync<Dossier>("GetDossier", param.DossierId);
            var emailBody = await context.CallActivityAsync<string>("GenerateEmail", dossier);
            return await context.CallActivityAsync<Email>("SendEmail", emailBody);
        }

        [FunctionName("GetDossier")]
        public static Dossier GetDossier([ActivityTrigger] Guid dossierId)
        {
            return Dossiers.FirstOrDefault(x => x.Id == dossierId);
        }


        [FunctionName("GenerateEmail")]
        public static string GenerateEmail([ActivityTrigger] Dossier dossier)
        {
            if (dossier == null)
            {
                return "Dossier null";
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{dossier.Address}");
            stringBuilder.AppendLine($"Dear {dossier.Name},");
            stringBuilder.AppendLine($"We are sorry to inform that Durable Function is too complex to process, so pls use another one!");
            stringBuilder.AppendLine($"Cheers,");
            stringBuilder.AppendLine($"Toan,");

            return stringBuilder.ToString();
        }

        [FunctionName("SendEmail")]
        public static Email SendEmail([ActivityTrigger] string body)
        {
            var newEmail = new Email
            {
                From = "toan-huu.nguyen@idealogic.com.vn",
                To = "thanh.tran@devinition.com.vn",
                Subject = "DON'T use Durable function pls",
                Body = body
            };

            // TODO send email

            return newEmail;
        }
    }
}
