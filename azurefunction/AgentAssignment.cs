using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;

namespace AgentAssignmentFunction
{
    public static class AssignAgentFunctionDataverse
    {
        [FunctionName("AssignAgentToTicketDataverse")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            var env = Environment.GetEnvironmentVariables();
            var clientId = env["DataverseClientId"].ToString();
            var clientSecret = env["DataverseSecret"].ToString();
            var tenantId = env["DataverseTenantId"].ToString();
            var dataverseUrl = env["DataverseUrl"].ToString();
            var openAiKey = env["OpenAiApiKey"].ToString();
            var openAiEndpoint = env["OpenAiEndpoint"].ToString();

            var body = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<TicketRequest>(body);

            // Step 1: Get AI category prediction
            string predictedCategory = await PredictCategory(data.Description, openAiEndpoint, openAiKey);

            // Step 2: Authenticate to Dataverse
            var accessToken = await GetDataverseAccessToken(clientId, clientSecret, tenantId, dataverseUrl);

            // Step 3: Get Category ID from Dataverse
            string categoryId = await GetCategoryId(dataverseUrl, accessToken, predictedCategory);

            // Step 4: Get Available Agent with that specialty
            string agentId = await GetAvailableAgent(dataverseUrl, accessToken, categoryId);

            if (agentId == null)
                return new NotFoundObjectResult("No available agent.");
            // Step 5: Assign agent to ticket
            await PatchTicket(dataverseUrl, accessToken, data.TicketID, agentId, categoryId, log);

            return new OkObjectResult(new { AssignedAgentID = agentId, Category = predictedCategory });

        }


        public class TicketRequest
        {
            public string TicketID { get; set; }
            public string Description { get; set; }
        }

        // Predict category using Azure OpenAI
        private static async Task<string> PredictCategory(string description, string endpoint, string apiKey)
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var content = new
            {
                messages = new[]
                {
                        new { role = "system", content = "Classify this helpdesk ticket into: Magic, Nature, or Battle." },
                        new { role = "user", content = description }
                    },
                temperature = 0.2,
                max_tokens = 10
            };

            var response = await client.PostAsync(
                    endpoint,
                    new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json")
                );

            var result = await response.Content.ReadAsStringAsync();
            dynamic json = JsonConvert.DeserializeObject(result);
            return json.choices[0].message.content.ToString();
        }

        // Auth to Dataverse via Client Credential Flow
        private static async Task<string> GetDataverseAccessToken(string clientId, string clientSecret, string tenantId, string dataverseUrl)
        {
            var tokenClient = new HttpClient();
            var dict = new Dictionary<string, string>
            {
                {"client_id", clientId },
                {"client_secret", clientSecret },
                {"grant_type", "client_credentials" },
                {"resource", dataverseUrl }
            };
            var tokenResponse = await tokenClient.PostAsync(
                $"https://login.microsoftonline.com/{tenantId}/oauth2/token",
                new FormUrlEncodedContent(dict)
            );

            var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
            return JObject.Parse(tokenContent)["access_token"].ToString();
        }

        private static async Task<string> GetCategoryId(string baseUrl, string token, string categoryName)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync($"{baseUrl}api/data/v9.2/cr132_categories?$filter=cr132_categoryname eq '{categoryName}'");
            var result = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(result);
            return json["value"]?.First?["cr132_categoryid"]?.ToString();
        }

        private static async Task<string> GetAvailableAgent(string baseUrl, string token, string categoryId)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync($"{baseUrl}api/data/v9.2/cr132_agents?$filter=cr132_isavailable eq true and _cr132_specialtycategory_id_value eq '{categoryId}'&$top=1");
            var result = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(result);
            return json["value"]?.First?["cr132_agentid"]?.ToString();
        }

        private static async Task PatchTicket(string baseUrl, string token, string ticketId, string agentId, string categoryID, ILogger log)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var payload = new Dictionary<string, object>
            {
                { "cr132_Agent_ID@odata.bind", $"/cr132_agents({agentId})" },
                { "cr132_Category_ID@odata.bind", $"/cr132_categories({categoryID})" }
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Patch, $"{baseUrl}api/data/v9.2/cr132_ticketses({ticketId})")
            {
                Content = content
            };
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                log.LogError($"PATCH failed: {response.StatusCode}\n{error}");
            }
            else
            {
                log.LogInformation("Ticket updated successfully.");
            }
        }
    }

}
