using MamMap.Application.System.Chat;
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
        private readonly IChatService _chatService;

        private const string ApiKey = "AIzaSyADLXdLtdYq8BdT8GFMDAd2Llc1a7Ef1cw";
        private const string ApiUrl =
            $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={ApiKey}";

        private readonly List<string> _reviewKeywords = new() { "đánh giá", "review", "nhận xét" };
        private readonly List<string> _greetingKeywords = new() { "xin chào", "chào", "hi", "hello", "alo", "ê" };
        private readonly List<string> _searchKeywords = new() { "quán ăn", "quán nào", "ngon", "khu vực", "quận", "gần đây", "ở đâu", "đói", "gợi ý" };
        private readonly List<string> _dishKeywords = new() { "món", "ăn", "thèm", "muốn ăn", "liệt kê", "có gì" };
        private readonly List<string> _dishInquiryKeywords = new() { "món gì", "bán gì", "có món gì", "thực đơn", "menu", "món ăn gì" };
        private readonly List<string> _highlyRatedKeywords = new() { "ngon nhất", "đánh giá cao", "tốt nhất", "quán top", "quán đỉnh", "quán hot", "quán rating cao" };
        private readonly List<string> _feeKeywords = new() { "phí", "thu phí", "giá", "tiền", "trả phí", "miễn phí", "cost", "fee", "price" };
        private readonly List<string> _attributeKeywords = new() { "cay", "không cay", "ít cay", "mặn", "nhạt", "ngọt", "chua", "đắng", "thơm", "thơm ngon", "đậm đà", "ăn chay", "chay", "thuần chay", "vegan", "vegetarian", "healthy", "lành mạnh", "ít dầu mỡ", "ít béo", "giảm cân", "low fat", "vị cay", "vị ngọt", "vị mặn", "vị đậm", "vị thơm", "vị chua", "vị đắng" };

        private static readonly HashSet<string> _noiseWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "quán","tiệm","cửa hàng","shop","hàng","chỗ","ngon",
            "nổi","tiếng","ở","tại","nào","đi","đến","giúp","tôi","cho","biết"
        };

        public GeminiService(IHttpClientFactory httpClientFactory, IChatService chatService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _chatService = chatService;
        }

        public async Task<(bool isSuccess, string message, string response)> GetBotResponseAsync(
            string prompt,
            string? userName,
            string userId,
            Guid? sessionId,
            List<SnackPlaces> snackPlaces,
            List<Reviews> reviews,
            List<Dishes> allDishes,
            List<SnackPlaceAttributes> attributes)
        {
            Guid currentSessionId = sessionId ?? Guid.Empty;
            string botResponse;

            try
            {
                // -------- 3.1. Session --------
                if (sessionId == null || sessionId == Guid.Empty)
                    currentSessionId = (await _chatService.CreateNewSessionAsync(userId)).SessionId;

                await _chatService.AddMessageToSessionAsync(currentSessionId, "User", prompt);

                var conversationHistory = (await _chatService.GetChatSessionByIdAsync(currentSessionId))
                                          ?.Messages.ToList() ?? new List<ChatMessage>();

                // -------- 3.2. Pre‑processing --------
                string normalizedPrompt = prompt.ToLower().Trim();
                bool isGreeting = _greetingKeywords.Contains(normalizedPrompt);
                bool askDishExplicitly = _dishInquiryKeywords.Any(k => normalizedPrompt.Contains(k));
                bool isReviewRequest = _reviewKeywords.Any(k => normalizedPrompt.Contains(k));
                bool isAttributeSearch = _attributeKeywords.Any(k => normalizedPrompt.Contains(k));

                // -------- 3.3. Greetings --------
                if (isGreeting)
                {
                    botResponse = $"Măm Map xin chào{(string.IsNullOrWhiteSpace(userName) ? "" : $" {userName}")}! " +
                                  "Bạn cần hỗ trợ gì về các quán ăn vặt?";
                    await _chatService.AddMessageToSessionAsync(currentSessionId, "Bot", botResponse);
                    return (true, "Thành công", botResponse);
                }

                // -------- 3.3.2 Matching Attributes --------
                if (isAttributeSearch)
                {
                    var matchedByAttrWithDetails = FindPlacesByAttribute(normalizedPrompt, snackPlaces, attributes);
                    if (matchedByAttrWithDetails.Any())
                    {
                        botResponse = await CallGeminiAPIForAttributes(prompt, userName, matchedByAttrWithDetails, conversationHistory);
                        await _chatService.AddMessageToSessionAsync(currentSessionId, "Bot", botResponse);
                        return (true, "Thành công", botResponse);
                    }
                }

                // -------- 3.4. Matching place --------
                SnackPlaces? matchedPlace = FindMatchingPlace(normalizedPrompt, snackPlaces);

                if (matchedPlace != null)
                {
                    // 3.4.1. Hỏi món => trả menu trực tiếp
                    if (askDishExplicitly)
                    {
                        botResponse = BuildDishListResponse(matchedPlace, allDishes);
                        await _chatService.AddMessageToSessionAsync(currentSessionId, "Bot", botResponse);
                        return (true, "Thành công", botResponse);
                    }

                    // 3.4.2. Hỏi review/khác => gọi Gemini
                    Reviews? latestReview = null;
                    if (isReviewRequest)
                    {
                        latestReview = reviews
                            .Where(r => r.SnackPlaceId == matchedPlace.SnackPlaceId && r.Status && !string.IsNullOrEmpty(r.Comment))
                            .OrderByDescending(r => r.ReviewDate)
                            .FirstOrDefault();
                    }

                    var dishesForPlace = allDishes
                        .Where(d => d.SnackPlaceId == matchedPlace.SnackPlaceId && d.Status)
                        .OrderByDescending(d => d.Price)
                        .Take(5)
                        .ToList();

                    botResponse = await CallGeminiAPIWithPlace(
                        prompt, userName, matchedPlace, latestReview, dishesForPlace, conversationHistory);

                    await _chatService.AddMessageToSessionAsync(currentSessionId, "Bot", botResponse);
                    return (true, "Thành công", botResponse);
                }

                // -------- 3.5. General queries --------
                botResponse = await HandleGeneralQueries(
                    prompt, userName, normalizedPrompt, snackPlaces, reviews, allDishes, conversationHistory);

                await _chatService.AddMessageToSessionAsync(currentSessionId, "Bot", botResponse);
                return (true, "Thành công", botResponse);
            }
            catch (Exception ex)
            {
                await _chatService.AddMessageToSessionAsync(currentSessionId, "Bot",
                    "Xin lỗi, đã có lỗi xảy ra. Vui lòng thử lại sau.");
                return (false, $"Lỗi xử lý: {ex.Message}", "Xin lỗi, đã có lỗi xảy ra. Vui lòng thử lại sau.");
            }
        }

        private SnackPlaces? FindMatchingPlace(string normalizedPrompt, IEnumerable<SnackPlaces> snackPlaces)
        {
            string processedPrompt = RemoveDiacritics(ExtractKeywords(normalizedPrompt));

            foreach (var place in snackPlaces)
            {
                string processedName = RemoveDiacritics(ExtractKeywords(place.PlaceName.ToLower()));
                if (string.IsNullOrEmpty(processedName)) continue;

                var promptWords = processedPrompt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var placeWords = processedName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                int common = promptWords.Intersect(placeWords).Count();

                bool overlapOK = common >= 1 &&
                                 ((double)common / promptWords.Length >= 0.5 ||
                                   (double)common / placeWords.Length >= 0.5);

                if (overlapOK ||
                    processedPrompt.Contains(processedName) ||
                    processedName.Contains(processedPrompt))
                    return place;
            }
            return null;
        }

        private string BuildDishListResponse(SnackPlaces place, IEnumerable<Dishes> allDishes)
        {
            var dishes = allDishes
                         .Where(d => d.SnackPlaceId == place.SnackPlaceId && d.Status)
                         .OrderByDescending(d => d.Price)
                         .ToList();

            if (!dishes.Any())
                return $"Hiện tại Măm Map chưa có menu nào của **{place.PlaceName}**.";

            var sb = new StringBuilder();
            sb.AppendLine($"📋 **Menu tại {place.PlaceName}:**");
            foreach (var d in dishes)
                sb.AppendLine($"• {d.Name} – {d.Price:N0} đ\n  {d.Description}");
            sb.AppendLine("Thử rồi chia sẻ cảm nhận trên Măm Map nhé!");
            return sb.ToString();
        }

        private async Task<string> HandleGeneralQueries(
            string prompt,
            string? userName,
            string normalizedPrompt,
            List<SnackPlaces> snackPlaces,
            List<Reviews> reviews,
            List<Dishes> allDishes,
            List<ChatMessage> conversationHistory)
        {
            // 1. Quán rating cao
            if (_highlyRatedKeywords.Any(k => normalizedPrompt.Contains(k)))
            {
                var top = GetTopRatedSnackPlaces(snackPlaces, reviews, 3);
                if (top.Any())
                    return await CallGeminiAPIForTopRated(prompt, userName, top, conversationHistory);

                return "Xin lỗi, Măm Map chưa có đủ dữ liệu đánh giá.";
            }

            // 2. Hỏi phí
            if (_feeKeywords.Any(k => normalizedPrompt.Contains(k)) && !normalizedPrompt.Contains("đánh giá"))
            {
                return "Măm Map **miễn phí** cho người dùng. " +
                       "Chúng tôi chỉ thu phí gói **Cơ bản** và **Tiêu chuẩn** dành cho chủ quán.";
            }

            // 3. Tìm quán tổng quát
            if (_searchKeywords.Any(k => normalizedPrompt.Contains(k)))
                return await CallGeminiAPI(prompt, userName, snackPlaces, conversationHistory);

            // 4. Mặc định
            return await CallGeminiAPI(prompt, userName, new List<SnackPlaces>(), conversationHistory);
        }

        private async Task<string> CallGeminiAPIWithPlace(
            string prompt,
            string? userName,
            SnackPlaces place,
            Reviews? review,
            List<Dishes> allDishes,
            List<ChatMessage> conversationHistory)
        {
            var infoText = new StringBuilder();
            var promptInstruction = @"
Bạn là Măm Map Bot, một trợ lý ảo chuyên tư vấn quán ăn vặt...";
            infoText.AppendLine($"Tên quán: {place.PlaceName}");
            infoText.AppendLine($"Địa chỉ: {place.Address}");
            if (!string.IsNullOrEmpty(place.Description))
                infoText.AppendLine($"Mô tả: {place.Description}");
            if (review != null && !string.IsNullOrEmpty(review.Comment))
                infoText.AppendLine($"Đánh giá gần nhất: {review.Comment}");
            if (allDishes.Any())
            {
                infoText.AppendLine("Danh sách món (không thêm món ngoài):");
                foreach (var dish in allDishes)
                    infoText.AppendLine($"- {dish.Name}: {dish.Description}");
            }
            else
            {
                infoText.AppendLine("Quán này chưa cập nhật menu.");
            }
            infoText.Insert(0, promptInstruction + "\n\n");

            var history = BuildHistory(conversationHistory);
            history.Add(new
            {
                role = "user",
                parts = new[] { new { text = $"Câu hỏi: {prompt}\n{infoText}" } }
            });

            string botReply = await SendGemini(history);

            return botReply;
        }

        private async Task<string> CallGeminiAPI(
            string prompt,
            string? userName,
            List<SnackPlaces> snackPlaces,
            List<ChatMessage> conversationHistory)
        {
            var initialPrompt = @"
                Bạn là Măm Map Bot, một trợ lý ảo...";
            var fullPrompt = new StringBuilder(initialPrompt);

            bool isGeneralSearch = _searchKeywords.Any(k => prompt.ToLower().Contains(k));
            if (isGeneralSearch && snackPlaces.Any())
            {
                fullPrompt.AppendLine("\nDanh sách quán:");
                foreach (var p in snackPlaces)
                    fullPrompt.AppendLine($"- {p.PlaceName} ({p.Address})");
            }
            fullPrompt.AppendLine($"\nCâu hỏi: {prompt}");

            var history = BuildHistory(conversationHistory);
            history.Add(new
            {
                role = "user",
                parts = new[] { new { text = fullPrompt.ToString() } }
            });

            string botReply = await SendGemini(history);

            return botReply;
        }

        private async Task<string> CallGeminiAPIForTopRated(
            string prompt,
            string? userName,
            List<(SnackPlaces place, double averageRating)> topRatedPlaces,
            List<ChatMessage> conversationHistory)
        {
            var instruction = @"
                Bạn là Măm Map Bot, một trợ lý ảo...";
            var info = new StringBuilder(instruction + "\n\nCác quán rating cao:");
            foreach (var (pl, avg) in topRatedPlaces)
            {
                info.AppendLine($"- {pl.PlaceName} ({pl.Address}) – ⭐ {avg:F1}/5");
                if (!string.IsNullOrEmpty(pl.Description))
                    info.AppendLine($"  {pl.Description}");
            }

            var history = BuildHistory(conversationHistory);
            history.Add(new
            {
                role = "user",
                parts = new[] { new { text = $"Câu hỏi: {prompt}\n{info}" } }
            });

            string botReply = await SendGemini(history);

            return botReply;
        }

        private async Task<string> CallGeminiAPIForAttributes(
            string prompt,
            string? userName,
            List<(SnackPlaces place, List<string> matchedAttributeDescriptions)> placesWithAttributes,
            List<ChatMessage> conversationHistory)
        {
            var initialPrompt = @"
        Bạn là Măm Map Bot, một trợ lý ảo chuyên tư vấn quán ăn vặt.
        Dưới đây là danh sách các quán ăn vặt được tìm thấy dựa trên các thuộc tính phù hợp với yêu cầu của người dùng.
        Hãy sử dụng thông tin này để trả lời câu hỏi của người dùng một cách hữu ích và tự nhiên, tập trung vào các thuộc tính đã khớp.
        Nếu không có quán nào phù hợp, hãy thông báo cho người dùng.";
            var fullPrompt = new StringBuilder(initialPrompt);

            fullPrompt.AppendLine("\nDanh sách quán phù hợp theo thuộc tính:");
            foreach (var (place, attrs) in placesWithAttributes)
            {
                fullPrompt.AppendLine($"- {place.PlaceName} ({place.Address})");
                if (!string.IsNullOrEmpty(place.Description))
                    fullPrompt.AppendLine($"  Mô tả: {place.Description}");
                fullPrompt.AppendLine($"  Thuộc tính nổi bật: {string.Join(", ", attrs.Select(a => $"'{a}'"))}");
            }
            fullPrompt.AppendLine($"\nCâu hỏi: {prompt}");

            var history = BuildHistory(conversationHistory);
            history.Add(new
            {
                role = "user",
                parts = new[] { new { text = fullPrompt.ToString() } }
            });

            string botReply = await SendGemini(history);
            //if (!string.IsNullOrWhiteSpace(userName) && botReply.Length > 1)
            //    botReply = $"Chào bạn {userName}, {char.ToLower(botReply[0])}{botReply.Substring(1)}";

            return botReply;
        }

        private async Task<string> SendGemini(List<object> history)
        {
            var reqBody = new { contents = history };
            using var content = new StringContent(JsonConvert.SerializeObject(reqBody), Encoding.UTF8, "application/json");
            var resp = await _httpClient.PostAsync(ApiUrl, content);
            resp.EnsureSuccessStatusCode();

            string json = await resp.Content.ReadAsStringAsync();
            dynamic res = JsonConvert.DeserializeObject(json);
            return res?.candidates?[0]?.content?.parts?[0]?.text ?? "Xin lỗi, Măm Map Bot chưa thể trả lời.";
        }

        private static List<object> BuildHistory(IEnumerable<ChatMessage> conversationHistory)
        {
            var history = new List<object>
            {
                new
                {
                    role  = "user",
                    parts = new[] { new { text = "Bạn là Măm Map Bot..." } }
                },
                new
                {
                    role  = "model",
                    parts = new[] { new { text = "Vâng, tôi đã hiểu." } }
                }
            };

            foreach (var msg in conversationHistory)
            {
                history.Add(new
                {
                    role = msg.Sender.Equals("user", StringComparison.OrdinalIgnoreCase) ? "user" : "model",
                    parts = new[] { new { text = msg.Text } }
                });
            }
            return history;
        }

        private static string ExtractKeywords(string input)
        {
            var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                             .Select(w => w.ToLower());

            var filtered = words.Where(w => !_noiseWords.Contains(w));
            return string.Join(' ', filtered).Trim();
        }

        private static string RemoveDiacritics(string input)
        {
            var normalized = input.Normalize(NormalizationForm.FormD);
            var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
            return new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
        }

        private List<(SnackPlaces place, double averageRating)> GetTopRatedSnackPlaces(
            List<SnackPlaces> snackPlaces,
            List<Reviews> reviews,
            int count)
        {
            var placeRatings = new Dictionary<Guid, List<double>>();

            foreach (var rv in reviews.Where(r => r.Status))
            {
                var ratings = new List<int>();
                if (rv.TasteRating > 0) ratings.Add(rv.TasteRating);
                if (rv.PriceRating > 0) ratings.Add(rv.PriceRating);
                if (rv.SanitaryRating > 0) ratings.Add(rv.SanitaryRating);
                if (rv.TextureRating > 0) ratings.Add(rv.TextureRating);
                if (rv.ConvenienceRating > 0) ratings.Add(rv.ConvenienceRating);

                if (!ratings.Any()) continue;

                double avg = ratings.Average();
                if (!placeRatings.ContainsKey(rv.SnackPlaceId))
                    placeRatings[rv.SnackPlaceId] = new List<double>();
                placeRatings[rv.SnackPlaceId].Add(avg);
            }

            return placeRatings
                .Select(p => new { p.Key, Avg = p.Value.Average() })
                .OrderByDescending(p => p.Avg)
                .Join(snackPlaces,
                      p => p.Key,
                      s => s.SnackPlaceId,
                      (p, s) => (place: s, averageRating: p.Avg))
                .Take(count)
                .ToList();
        }

        private List<(SnackPlaces place, List<string> matchedAttributeDescriptions)> FindPlacesByAttribute(
            string normalizedPrompt,
            List<SnackPlaces> snackPlaces,
            List<SnackPlaceAttributes> attributes)
        {
            string processedPrompt = RemoveDiacritics(normalizedPrompt);
            var results = new List<(SnackPlaces place, List<string> matchedAttributeDescriptions)>();

            var attributesByPlaceId = attributes.GroupBy(attr => attr.SnackPlaceId)
                                                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var place in snackPlaces)
            {
                if (attributesByPlaceId.TryGetValue(place.SnackPlaceId, out var placeAttributes))
                {
                    var currentPlaceMatchedAttrs = new List<string>();

                    foreach (var attr in placeAttributes)
                    {
                        var taste = attr.Taste?.Description?.ToLowerInvariant();
                        var diet = attr.Diet?.Description?.ToLowerInvariant();
                        var type = attr.FoodType?.Description?.ToLowerInvariant();

                        if (!string.IsNullOrEmpty(taste) && processedPrompt.Contains(RemoveDiacritics(taste)))
                        {
                            currentPlaceMatchedAttrs.Add(taste);
                        }
                        if (!string.IsNullOrEmpty(diet) && processedPrompt.Contains(RemoveDiacritics(diet)))
                        {
                            currentPlaceMatchedAttrs.Add(diet);
                        }
                        if (!string.IsNullOrEmpty(type) && processedPrompt.Contains(RemoveDiacritics(type)))
                        {
                            currentPlaceMatchedAttrs.Add(type);
                        }
                    }

                    if (currentPlaceMatchedAttrs.Any())
                    {
                        results.Add((place, currentPlaceMatchedAttrs.Distinct().ToList()));
                    }
                }
            }
            return results;
        }
    }
}
