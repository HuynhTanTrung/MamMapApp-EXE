using MamMap.Data.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MamMap.Application.System.Gemini
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private const string ApiKey = "AIzaSyADLXdLtdYq8BdT8GFMDAd2Llc1a7Ef1cw";
        private const string ApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={ApiKey}";

        private readonly List<string> _reviewKeywords = new() { "đánh giá", "review", "nhận xét" };
        private readonly List<string> _greetingKeywords = new() { "xin chào", "chào", "hi", "hello", "alo", "ê" };
        private readonly List<string> _searchKeywords = new() { "quán ăn", "quán nào", "ngon", "khu vực", "quận", "gần đây", "ở đâu" };

        public GeminiService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<(bool isSuccess, string message, string response)> GetBotResponseAsync(
            string prompt,
            string? userName,
            List<SnackPlaces> snackPlaces,
            List<Reviews> reviews,
            List<Dishes> allDishes)
        {
            try
            {
                var normalizedPrompt = RemoveDiacritics(prompt.ToLower());
                var promptWords = normalizedPrompt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var greetingName = !string.IsNullOrWhiteSpace(userName) ? $" {userName}" : "";
                var promptPhrases = new List<string>();

                // Sinh 2-word phrases như cũ
                for (int i = 0; i < promptWords.Length - 1; i++)
                {
                    promptPhrases.Add($"{promptWords[i]} {promptWords[i + 1]}");
                }

                // Thêm cả từ đơn
                promptPhrases.AddRange(promptWords);

                // Handle greeting
                if (_greetingKeywords.Contains(normalizedPrompt))
                {
                    return (true, "Thành công", $"Măm Map xin chào{greetingName}! Tôi là Măm Map Bot, bạn cần hỗ trợ gì về nền tảng review quán ăn vặt Măm Map ạ?");
                }

                // Check for review-related question
                bool isReviewRequest = _reviewKeywords.Any(keyword => normalizedPrompt.Contains(keyword));

                // Try to find a matching snack place by name
                var matchingPlace = snackPlaces.FirstOrDefault(place =>
                {
                    var promptText = RemoveDiacritics(prompt.ToLower());
                    var placeText = RemoveDiacritics(place.PlaceName.ToLower());

                    return placeText.Contains(promptText) || promptText.Contains(placeText);
                });

                // If no place matched by name, try matching via dishes
                if (matchingPlace == null)
                {
                    var cleanedPrompt = RemoveDiacritics(prompt.ToLower());
                    var promptTokens = cleanedPrompt.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var matchedDishList = allDishes
                        .Where(d => d.Status)
                        .Select(d => new
                        {
                            Dish = d,
                            Score = promptTokens.Count(pt =>
                                RemoveDiacritics(d.Name.ToLower()).Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                    .Any(dt => pt == dt))
                        })
                        .Where(x => x.Score >= 2) // At least 2 word matches to reduce false positives
                        .OrderByDescending(x => x.Score)
                        .ToList();

                    if (matchedDishList.Any())
                    {
                        var bestDish = matchedDishList.First().Dish;
                        matchingPlace = snackPlaces.FirstOrDefault(p => p.SnackPlaceId == bestDish.SnackPlaceId);
                    }
                }


                if (matchingPlace != null)
                {
                    Reviews? latestReview = null;

                    if (isReviewRequest)
                    {
                        latestReview = reviews
                            .Where(r => r.SnackPlaceId == matchingPlace.SnackPlaceId && r.Status && !string.IsNullOrEmpty(r.Comment))
                            .OrderByDescending(r => r.ReviewDate)
                            .FirstOrDefault();
                    }

                    var dishesForPlace = allDishes
                        .Where(d => d.SnackPlaceId == matchingPlace.SnackPlaceId && d.Status)
                        .OrderByDescending(d => d.Price)
                        .Take(5)
                        .ToList();

                    var aiResponse = await CallGeminiAPIWithPlace(prompt, userName, matchingPlace, latestReview, dishesForPlace);
                    return (true, "Thành công", aiResponse);
                }

                // No matching place found – fallback to general Gemini API
                var fallbackResponse = await CallGeminiNoPlaceAPI(prompt, userName);
                return (true, "Thành công", fallbackResponse);
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi xử lý: {ex.Message}", "");
            }
        }

        private async Task<string> CallGeminiAPIWithPlace(string prompt, string? userName, SnackPlaces place, Reviews? review, List<Dishes> allDishes)
        {
            var infoText = new StringBuilder();

            var promptInstruction = @"
            Bạn là Măm Map Bot, một trợ lý ảo chuyên tư vấn quán ăn vặt.

            ⚠️ Tuyệt đối KHÔNG được bịa thêm món ăn nào ngoài danh sách món ăn được cung cấp bên dưới.

            - Nếu không có món nào khác, thì chỉ nói đúng các món đã có.
            - Không được nói những món 'thỉnh thoảng có', 'có thể có', hoặc 'ngoài ra còn có thể có'.
            - Phải trả lời như người thật từng ăn, thân thiện và tự nhiên.

            Mục tiêu là giới thiệu quán như một người từng trải nghiệm, nhưng chỉ dựa vào dữ liệu đã cho.";

            infoText.AppendLine($"Tên quán: {place.PlaceName}");
            infoText.AppendLine($"Địa chỉ: {place.Address}");
            if (!string.IsNullOrEmpty(place.Description))
                infoText.AppendLine($"Mô tả: {place.Description}");
            if (review != null && !string.IsNullOrEmpty(review.Comment))
                infoText.AppendLine($"Đánh giá gần nhất: {review.Comment}");

            var dishes = allDishes
                .Where(d => d.SnackPlaceId == place.SnackPlaceId && d.Status)
                .ToList();

            if (dishes.Any())
            {
                infoText.AppendLine("Danh sách món ăn tại quán:");
                foreach (var dish in dishes)
                {
                    infoText.AppendLine($"- {dish.Name}: {dish.Description}");
                }
            }

            infoText.Insert(0, promptInstruction + "\n\n");

            var history = new List<object>
            {
                new
                {
                    role = "user",
                    parts = new[] {
                        new {
                            text = "Bạn là Măm Map Bot, một trợ lý ảo của nền tảng Măm Map. Măm Map là một hệ thống giúp người dùng tìm kiếm và đánh giá các quán ăn vặt. Dựa trên thông tin dưới đây về một quán ăn, hãy trả lời một cách thân thiện, tự nhiên và hữu ích như một người từng trải nghiệm quán. Không cần liệt kê máy móc, hãy diễn đạt như một người hiểu rõ về quán."
                        }
                    }
                },
                new
                {
                    role = "model",
                    parts = new[] {
                        new { text = "Vâng, tôi đã hiểu. Tôi sẽ trả lời một cách thân thiện dựa trên thông tin quán bên dưới." }
                    }
                },
                new
                {
                    role = "user",
                    parts = new[] {
                        new {
                            text = $"Câu hỏi của người dùng: {prompt}\nThông tin quán:\n{infoText}"
                        }
                    }
                }
            };

            var requestBody = new { contents = history };
            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            var res = await _httpClient.PostAsync(ApiUrl, content);
            res.EnsureSuccessStatusCode();

            var resString = await res.Content.ReadAsStringAsync();
            dynamic resJson = JsonConvert.DeserializeObject(resString);

            string botReply = resJson?.candidates?[0]?.content?.parts?[0]?.text ?? "Xin lỗi, Măm Map Bot chưa thể trả lời yêu cầu của bạn.";

            if (!string.IsNullOrWhiteSpace(userName) && botReply.Length > 5)
            {
                botReply = $"Chào bạn {userName}, {char.ToLower(botReply[0])}{botReply.Substring(1)}";
            }

            return botReply;
        }

        private async Task<string> CallGeminiNoPlaceAPI(string prompt, string? userName)
        {
            var instruction = @"
            Bạn là Măm Map Bot, trợ lý ảo thân thiện của nền tảng Măm Map.

            Người dùng vừa hỏi về một món ăn hoặc quán ăn, tuy nhiên hiện tại **không có quán nào trong hệ thống** phù hợp với nội dung họ hỏi.

            ⚠️ Yêu cầu:
            - KHÔNG được bịa tên quán.
            - KHÔNG gợi ý quán nào ngoài hệ thống.
            - Chỉ trả lời rằng hiện tại Măm Map chưa có thông tin phù hợp.
            - Có thể khuyến khích người dùng thử món/quán khác.
            - Giữ giọng văn thân thiện, tự nhiên, như một người đang trò chuyện.";


            var history = new List<object>
    {
        new
        {
            role = "user",
            parts = new[] {
                new { text = instruction }
            }
        },
        new
        {
            role = "model",
            parts = new[] {
                new { text = "Vâng, tôi đã hiểu. Tôi sẽ trả lời thật thân thiện và không bịa thông tin quán." }
            }
        },
        new
        {
            role = "user",
            parts = new[] {
                new { text = $"Người dùng hỏi: {prompt}" }
            }
        }
    };

            var requestBody = new { contents = history };
            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            var res = await _httpClient.PostAsync(ApiUrl, content);
            res.EnsureSuccessStatusCode();

            var resString = await res.Content.ReadAsStringAsync();
            dynamic resJson = JsonConvert.DeserializeObject(resString);

            string botReply = resJson?.candidates?[0]?.content?.parts?[0]?.text
                ?? "Xin lỗi, Măm Map Bot chưa thể trả lời yêu cầu của bạn.";

            if (!string.IsNullOrWhiteSpace(userName) && botReply.Length > 5)
            {
                botReply = $"Chào bạn {userName}, {char.ToLower(botReply[0])}{botReply.Substring(1)}";
            }

            return botReply;
        }

        private async Task<string> CallGeminiAPI(string prompt, string? userName)
        {
            var history = new List<object>
            {
                new
                {
                    role = "user",
                    parts = new[] {
                        new {
                            text = "Bạn là Măm Map Bot, một trợ lý ảo của nền tảng Măm Map. Măm Map là một hệ thống trung gian giúp các chủ quán ăn vặt đăng ký và quản lý thông tin quán của họ, đồng thời cho phép người dùng tìm kiếm, đánh giá và review các quán ăn vặt. Mọi câu trả lời của bạn phải liên quan đến cách sử dụng nền tảng Măm Map, lợi ích cho chủ quán, cách review, cách tìm quán, giải quyết vấn đề trên nền tảng. Nếu người dùng hỏi về quán ăn cụ thể hoặc món ăn ngon ở một khu vực, hãy hướng dẫn họ cách sử dụng tính năng tìm kiếm trên Măm Map để tìm quán phù hợp. Tuyệt đối không tư vấn về món ăn cụ thể của một quán hoặc gợi ý quán ăn ngoài nền tảng. Luôn giữ thái độ thân thiện, chuyên nghiệp và hữu ích."
                        }
                    }
                },
                new
                {
                    role = "model",
                    parts = new[] {
                        new { text = "Vâng, tôi đã hiểu rõ. Măm Map xin chào! Tôi là Măm Map Bot, sẵn sàng hỗ trợ bạn về cách sử dụng nền tảng review quán ăn vặt Măm Map. Bạn có câu hỏi nào cho tôi không?" }
                    }
                },
                new
                {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            };

            var requestBody = new { contents = history };
            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            var res = await _httpClient.PostAsync(ApiUrl, content);
            res.EnsureSuccessStatusCode();

            var resString = await res.Content.ReadAsStringAsync();
            dynamic resJson = JsonConvert.DeserializeObject(resString);

            string botReply = resJson?.candidates?[0]?.content?.parts?[0]?.text ?? "Xin lỗi, Măm Map Bot chưa thể trả lời yêu cầu của bạn.";

            if (!string.IsNullOrWhiteSpace(userName) && botReply.Length > 5)
            {
                botReply = $"Chào bạn {userName}, {char.ToLower(botReply[0])}{botReply.Substring(1)}";
            }

            return botReply;
        }

        private static string ExtractKeywords(string input)
        {
            var keywords = input.ToLower()
                .Replace("quán ăn", "")
                .Replace("quán", "")
                .Replace("tiệm", "")
                .Trim();
            return keywords;
        }

        private static string RemoveDiacritics(string input)
        {
            var normalized = input.Normalize(NormalizationForm.FormD);
            var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
            return new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
        }
    }
}
