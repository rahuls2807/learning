using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WorkerBookingSystem.Models.ViewModels;

namespace WorkerBookingSystem.Controllers
{
    public class ChatbotController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<ChatbotController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ask([FromForm] ChatbotRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ChatbotResponse
                {
                    Reply = "Please ask a shorter question about booking, payments, workers, or admin tasks."
                });
            }

            var apiKey = _configuration["OpenAI:ApiKey"] ?? _configuration["OPENAI_API_KEY"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return Json(new ChatbotResponse
                {
                    Reply = GetLocalHelp(request.Message),
                    UsedAi = false
                });
            }

            try
            {
                var reply = await AskOpenAi(request, apiKey);
                return Json(new ChatbotResponse { Reply = reply, UsedAi = true });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI chatbot request failed.");
                return Json(new ChatbotResponse
                {
                    Reply = GetLocalHelp(request.Message),
                    UsedAi = false
                });
            }
        }

        private async Task<string> AskOpenAi(ChatbotRequest request, string apiKey)
        {
            var model = _configuration["OpenAI:Model"] ?? "gpt-4.1-mini";
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var instructions = """
                You are the in-app help assistant for Worker Booking System.
                Be concise, friendly, and practical. Help users understand how to register,
                book workers, update booking status, record cash paid to workers, pay online,
                view worker jobs, and use admin dashboards. Do not ask for card numbers,
                passwords, API keys, or private customer data. If asked for account-specific
                data, tell the user to open the relevant page after logging in.
                """;

            var body = new
            {
                model,
                instructions,
                max_output_tokens = 350,
                input = $"""
                    Current app page: {request.Page ?? "Unknown"}
                    User role hints: Admin links appear only to admins; clients see their bookings; workers see jobs.
                    User question: {request.Message}
                    """
            };

            using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var response = await client.PostAsync("https://api.openai.com/v1/responses", content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"OpenAI request failed: {(int)response.StatusCode}");
            }

            return ExtractResponseText(responseText);
        }

        private static string ExtractResponseText(string responseText)
        {
            using var document = JsonDocument.Parse(responseText);
            var root = document.RootElement;

            if (root.TryGetProperty("output_text", out var outputText)
                && outputText.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(outputText.GetString()))
            {
                return outputText.GetString()!;
            }

            if (root.TryGetProperty("output", out var outputItems) && outputItems.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in outputItems.EnumerateArray())
                {
                    if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var part in content.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                        {
                            var value = text.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                return value;
                            }
                        }
                    }
                }
            }

            return "I could not generate a clear answer. Try asking about booking, payments, worker jobs, or admin reports.";
        }

        private static string GetLocalHelp(string message)
        {
            var text = message.ToLowerInvariant();

            if (text.Contains("admin"))
                return "Admins can log in, open the Admin menu, manage rates, bookings, reports, and create more admins from Manage Admins.";
            if (text.Contains("pay") || text.Contains("cash") || text.Contains("balance"))
                return "Clients can open My Bookings, pay online for any remaining balance, or record cash paid directly to the worker. Admins can see online paid, cash paid, balance, worker pay, and profit.";
            if (text.Contains("status"))
                return "Clients can update their own booking status from My Bookings. Admins can update booking status from Manage Bookings.";
            if (text.Contains("book"))
                return "To book a worker, log in as a client, choose Book a Worker, search by skill or name, select Book Now, choose the date/time, and confirm payment.";
            if (text.Contains("worker") || text.Contains("job"))
                return "Workers should register or log in, then use My Jobs to view assigned work and their 90% payout. Clients can search workers by name or skill before booking.";

            return "I can help with booking workers, client payments, cash paid to workers, worker jobs, admin reports, and login access. What would you like to do?";
        }
    }
}
