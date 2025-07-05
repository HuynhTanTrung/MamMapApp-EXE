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
        private readonly List<string> _searchKeywords = new() { "quán ăn", "quán nào", "ngon", "khu vực", "quận", "gần đây", "ở đâu", "đói", "gợi ý" };
        private readonly List<string> _dishKeywords = new() { "món", "ăn", "thèm", "muốn ăn", "liệt kê", "có gì" };

        // TAO THÊM VÀO: Keyword để check câu hỏi về phí
        private readonly List<string> _feeKeywords = new() { "phí", "thu phí", "giá", "tiền", "trả phí", "miễn phí", "cost", "fee", "price" };

        private static readonly HashSet<string> _noiseWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "quán", "ăn", "tiệm", "cửa hàng", "shop", "hàng", "chỗ", "ngon", "nổi", "tiếng", "ở", "tại", "nào", "mày", "tôi", "cho"
        };

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
                var normalizedPrompt = prompt.ToLower().Trim();
                var greetingName = !string.IsNullOrWhiteSpace(userName) ? $" {userName}" : "";

                // 1. Handle greeting
                if (_greetingKeywords.Contains(normalizedPrompt))
                {
                    return (true, "Thành công", $"Măm Map xin chào{greetingName}! Tôi là Măm Map Bot, bạn cần hỗ trợ gì về nền tảng review quán ăn vặt Măm Map ạ?");
                }

                // TAO THÊM VÀO: Handle questions about fees
                if (_feeKeywords.Any(keyword => normalizedPrompt.Contains(keyword)))
                {
                    var feeResponse = "Chào bạn, Măm Map hoàn toàn **miễn phí** cho người dùng tìm kiếm nhé. " +
                                      "Tụi mình chỉ thu phí đối với các **chủ quán (merchant)** với 2 gói dịch vụ là **Cơ bản** và **Tiêu chuẩn** để quản lý và quảng bá quán hiệu quả hơn. " +
                                      "Nếu bạn là chủ quán và muốn biết thêm chi tiết, hãy tải app về để tìm hiểu kỹ hơn nha!";
                    return (true, "Thành công", feeResponse);
                }

                // Logic còn lại giữ nguyên...
                bool isReviewRequest = _reviewKeywords.Any(keyword => normalizedPrompt.Contains(keyword));
                bool isGeneralSearchOrHungry = _searchKeywords.Any(keyword => normalizedPrompt.Contains(keyword));
                bool isAskingAboutDishes = _dishKeywords.Any(keyword => normalizedPrompt.Contains(keyword));

                SnackPlaces? matchingPlace = null;
                Reviews? latestReview = null;
                List<Dishes> dishesForPlace = new List<Dishes>();

                var processedPromptForPlaceSearch = RemoveDiacritics(ExtractKeywords(normalizedPrompt));

                // 2. Ưu tiên tìm kiếm quán theo tên
                foreach (var place in snackPlaces)
                {
                    var processedPlaceName = RemoveDiacritics(ExtractKeywords(place.PlaceName.ToLower()));

                    if (processedPromptForPlaceSearch.Contains(processedPlaceName) ||
                        processedPlaceName.Contains(processedPromptForPlaceSearch) ||
                        normalizedPrompt.Contains(place.PlaceName.ToLower()) ||
                        place.PlaceName.ToLower().Contains(normalizedPrompt))
                    {
                        matchingPlace = place;
                        break;
                    }
                }

                // 3. Nếu tìm thấy quán cụ thể
                if (matchingPlace != null)
                {
                    if (isReviewRequest)
                    {
                        latestReview = reviews
                            .Where(r => r.SnackPlaceId == matchingPlace.SnackPlaceId && r.Status && !string.IsNullOrEmpty(r.Comment))
                            .OrderByDescending(r => r.ReviewDate)
                            .FirstOrDefault();
                    }

                    dishesForPlace = allDishes
                        .Where(d => d.SnackPlaceId == matchingPlace.SnackPlaceId && d.Status)
                        .OrderByDescending(d => d.Price)
                        .Take(5)
                        .ToList();

                    var aiResponse = await CallGeminiAPIWithPlace(prompt, userName, matchingPlace, latestReview, dishesForPlace);
                    return (true, "Thành công", aiResponse);
                }

                // 4. Nếu không tìm thấy quán cụ thể, thử tìm qua món ăn
                if (matchingPlace == null && isAskingAboutDishes)
                {
                    foreach (var dish in allDishes)
                    {
                        var normalizedDishName = dish.Name.ToLower();
                        if ((normalizedPrompt.Contains(normalizedDishName) || RemoveDiacritics(normalizedPrompt).Contains(RemoveDiacritics(normalizedDishName))))
                        {
                            matchingPlace = snackPlaces.FirstOrDefault(p => p.SnackPlaceId == dish.SnackPlaceId);
                            if (matchingPlace != null)
                            {
                                dishesForPlace = allDishes
                                    .Where(d => d.SnackPlaceId == matchingPlace.SnackPlaceId && d.Status)
                                    .OrderByDescending(d => d.Price)
                                    .Take(5)
                                    .ToList();

                                if (isReviewRequest)
                                {
                                    latestReview = reviews
                                        .Where(r => r.SnackPlaceId == matchingPlace.SnackPlaceId && r.Status && !string.IsNullOrEmpty(r.Comment))
                                        .OrderByDescending(r => r.ReviewDate)
                                        .FirstOrDefault();
                                }
                                var aiResponse = await CallGeminiAPIWithPlace(prompt, userName, matchingPlace, latestReview, dishesForPlace);
                                return (true, "Thành công", aiResponse);
                            }
                        }
                    }
                }

                // 5. Fallback: tìm kiếm chung hoặc câu hỏi khác
                if (isGeneralSearchOrHungry)
                {
                    var fallbackResponse = await CallGeminiAPI(prompt, userName, snackPlaces);
                    return (true, "Thành công", fallbackResponse);
                }
                else
                {
                    var fallbackResponse = await CallGeminiAPI(prompt, userName, new List<SnackPlaces>());
                    return (true, "Thành công", fallbackResponse);
                }
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
            Mục tiêu của bạn là giới thiệu quán như một người từng trải nghiệm, thân thiện và tự nhiên.

            Hãy trả lời bằng cách tổng hợp thông tin về quán, địa chỉ, và các món ăn (nếu có).
            Nếu có đánh giá gần nhất, hãy lồng ghép nó vào câu trả lời một cách tự nhiên.
            Tuyệt đối KHÔNG được bịa thêm bất kỳ món ăn nào ngoài danh sách món ăn được cung cấp.
            Chỉ nói đúng các món đã có.
            Không được nói những món 'thỉnh thoảng có', 'có thể có', hoặc 'ngoài ra còn có thể có'.
            Luôn giữ giọng điệu thân thiện, nhiệt tình và như một người bạn gợi ý.
            ";

            infoText.AppendLine($"Tên quán: {place.PlaceName}");
            infoText.AppendLine($"Địa chỉ: {place.Address}");
            if (!string.IsNullOrEmpty(place.Description))
                infoText.AppendLine($"Mô tả: {place.Description}");
            if (review != null && !string.IsNullOrEmpty(review.Comment))
                infoText.AppendLine($"Đánh giá gần nhất: {review.Comment}");

            if (allDishes.Any())
            {
                infoText.AppendLine("Danh sách món ăn tại quán (chỉ liệt kê các món này, không thêm):");
                foreach (var dish in allDishes)
                {
                    infoText.AppendLine($"- {dish.Name}: {dish.Description}");
                }
            }
            else
            {
                infoText.AppendLine("Quán này hiện chưa có thông tin món ăn nào được cập nhật trên Măm Map. Có thể quán mới hoặc chưa cập nhật đầy đủ menu.");
            }

            infoText.Insert(0, promptInstruction + "\n\n");

            var history = new List<object>
            {
                new
                {
                    role = "user",
                    parts = new[] {
                        new {
                            text = "Bạn là Măm Map Bot, một trợ lý ảo của nền tảng Măm Map. Măm Map là một hệ thống giúp người dùng tìm kiếm và đánh giá các quán ăn vặt. Dựa trên thông tin dưới đây về một quán ăn, hãy trả lời một cách thân thiện, tự nhiên và hữu ích như một người từng trải nghiệm quán. Không cần liệt kê máy móc, hãy diễn đạt như một người hiểu rõ về quán. Đừng bịa thêm món ăn hoặc thông tin không có."
                        }
                    }
                },
                new
                {
                    role = "model",
                    parts = new[] {
                        new { text = "Vâng, tôi đã hiểu. Tôi sẽ trả lời một cách thân thiện dựa trên thông tin quán bên dưới và tuyệt đối không bịa thêm thông tin." }
                    }
                },
                new
                {
                    role = "user",
                    parts = new[] {
                        new {
                            text = $"Câu hỏi của người dùng: {prompt}\nThông tin quán bạn cần tư vấn:\n{infoText}"
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

        private async Task<string> CallGeminiAPI(string prompt, string? userName, List<SnackPlaces> snackPlaces)
        {
            var initialPrompt = @"
            Bạn là Măm Map Bot, một trợ lý ảo của nền tảng Măm Map. Măm Map là một hệ thống trung gian giúp các chủ quán ăn vặt đăng ký và quản lý thông tin quán của họ, đồng thời cho phép người dùng tìm kiếm, đánh giá và review các quán ăn vặt.
            Mọi câu trả lời của bạn phải liên quan đến cách sử dụng nền tảng Măm Map, lợi ích cho chủ quán, cách review, cách tìm quán, giải quyết vấn đề trên nền tảng.
            Tuyệt đối không tư vấn về món ăn cụ thể của một quán hoặc gợi ý quán ăn ngoài nền tảng nếu không có trong dữ liệu bạn được cung cấp.
            Luôn giữ thái độ thân thiện, chuyên nghiệp và hữu ích.
            ";

            var fullPrompt = new StringBuilder(initialPrompt);

            bool isGeneralSearch = _searchKeywords.Any(keyword => prompt.ToLower().Contains(keyword));

            if (isGeneralSearch && snackPlaces.Any())
            {
                fullPrompt.AppendLine("\nNếu người dùng hỏi về quán ăn, hãy gợi ý các quán từ danh sách sau. Hãy chọn 3-5 quán nổi bật hoặc ngẫu nhiên từ danh sách và giới thiệu sơ lược về chúng, kèm theo địa chỉ. Đừng đưa ra danh sách quá dài, chỉ gợi ý những quán nổi bật nhất hoặc phù hợp với ngữ cảnh nếu có. Nếu không có ngữ cảnh, hãy đưa ra một vài quán ngẫu nhiên.");
                fullPrompt.AppendLine("Danh sách các quán ăn vặt hiện có trên Măm Map:");
                foreach (var place in snackPlaces)
                {
                    fullPrompt.AppendLine($"- Tên: {place.PlaceName}, Địa chỉ: {place.Address}");
                }
                fullPrompt.AppendLine($"\nCâu hỏi của người dùng: {prompt}");
            }
            else
            {
                fullPrompt.AppendLine($"\nCâu hỏi của người dùng: {prompt}");
            }

            var history = new List<object>
            {
                new
                {
                    role = "user",
                    parts = new[] {
                        new {
                            text = "Bạn là Măm Map Bot, một trợ lý ảo của nền tảng Măm Map. Hãy trả lời câu hỏi của người dùng dựa trên vai trò của bạn. Nếu câu hỏi liên quan đến tìm quán ăn, hãy gợi ý các quán có sẵn hoặc hướng dẫn cách tìm trên Măm Map. Luôn thân thiện và hữu ích."
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
                    parts = new[] { new { text = fullPrompt.ToString() } }
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
            var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(word => word.ToLower());

            var filteredWords = words.Where(word => !_noiseWords.Contains(word));

            return string.Join(" ", filteredWords).Trim();
        }

        private static string RemoveDiacritics(string input)
        {
            var normalized = input.Normalize(NormalizationForm.FormD);
            var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
            return new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
        }
    }
}