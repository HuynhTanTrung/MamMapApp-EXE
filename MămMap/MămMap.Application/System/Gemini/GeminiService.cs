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
        private const string ApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={ApiKey}";

        private readonly List<string> _reviewKeywords = new() { "đánh giá", "review", "nhận xét" };
        private readonly List<string> _greetingKeywords = new() { "xin chào", "chào", "hi", "hello", "alo", "ê" };
        private readonly List<string> _searchKeywords = new() { "quán ăn", "quán nào", "ngon", "khu vực", "quận", "gần đây", "ở đâu", "đói", "gợi ý" };
        private readonly List<string> _dishKeywords = new() { "món", "ăn", "thèm", "muốn ăn", "liệt kê", "có gì" };
        private readonly List<string> _highlyRatedKeywords = new() { "ngon nhất", "đánh giá cao", "tốt nhất", "quán top", "quán đỉnh", "quán hot", "quán rating cao" };
        private readonly List<string> _feeKeywords = new() { "phí", "thu phí", "giá", "tiền", "trả phí", "miễn phí", "cost", "fee", "price" };

        // ĐÃ THAY ĐỔI: Thu hẹp danh sách từ nhiễu
        private static readonly HashSet<string> _noiseWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "quán", "tiệm", "cửa hàng", "shop", "hàng", "chỗ", "ngon", "nổi", "tiếng", "ở", "tại", "nào", "đi", "đến", "giúp", "tôi", "cho", "biết"
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
            List<Dishes> allDishes)
        {
            Guid currentSessionId = sessionId ?? Guid.Empty;
            string botResponse = "";

            try
            {
                if (sessionId == null || sessionId == Guid.Empty)
                {
                    var newSession = await _chatService.CreateNewSessionAsync(userId);
                    currentSessionId = newSession.SessionId;
                }

                await _chatService.AddMessageToSessionAsync(currentSessionId, "User", prompt);

                var currentSession = await _chatService.GetChatSessionByIdAsync(currentSessionId);
                var existingMessages = currentSession?.Messages.ToList() ?? new List<ChatMessage>();
                var normalizedPrompt = prompt.ToLower().Trim();
                var greetingName = !string.IsNullOrWhiteSpace(userName) ? $" {userName}" : "";

                // 1. Handle greeting - Khôi phục lại lời chào cố định
                if (_greetingKeywords.Contains(normalizedPrompt))
                {
                    botResponse = $"Măm Map xin chào{greetingName}! Tôi là Măm Map Bot, bạn cần hỗ trợ gì về nền tảng review quán ăn vặt Măm Map ạ?";
                }
                else
                {
                    bool isReviewRequest = _reviewKeywords.Any(keyword => normalizedPrompt.Contains(keyword));
                    bool isGeneralSearchOrHungry = _searchKeywords.Any(keyword => normalizedPrompt.Contains(keyword));
                    bool isAskingAboutDishes = _dishKeywords.Any(keyword => normalizedPrompt.Contains(keyword));

                    SnackPlaces? matchingPlace = null;
                    Reviews? latestReview = null;
                    List<Dishes> dishesForPlace = new List<Dishes>();

                    var processedPromptForPlaceSearch = RemoveDiacritics(ExtractKeywords(normalizedPrompt));

                    Console.WriteLine($"DEBUG: Processed Prompt for Place Search: '{processedPromptForPlaceSearch}'");

                    // 2. Ưu tiên tìm kiếm quán theo tên (hoặc qua món ăn)
                    // Logic này cần được ưu tiên hơn các từ khóa chung như phí
                    // Attempt to find a place by name
                    foreach (var place in snackPlaces)
                    {
                        var processedPlaceName = RemoveDiacritics(ExtractKeywords(place.PlaceName.ToLower()));

                        Console.WriteLine($"DEBUG: Comparing Prompt with Place: '{place.PlaceName}' (Processed: '{processedPlaceName}')");

                        var promptWords = processedPromptForPlaceSearch.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var placeNameWords = processedPlaceName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                        bool hasStrongWordOverlap = false;
                        if (promptWords.Length > 0 && placeNameWords.Length > 0)
                        {
                            var commonWords = promptWords.Intersect(placeNameWords).Count();
                            double overlapPercentagePrompt = (double)commonWords / promptWords.Length;
                            double overlapPercentagePlace = (double)commonWords / placeNameWords.Length;

                            if (overlapPercentagePrompt >= 0.5 || overlapPercentagePlace >= 0.5 ||
                                (commonWords >= 1 && (promptWords.Length > 1 || placeNameWords.Length > 1)))
                            {
                                hasStrongWordOverlap = true;
                            }
                        }

                        bool foundByProcessedContains = processedPromptForPlaceSearch.Contains(processedPlaceName) ||
                                                        processedPlaceName.Contains(processedPromptForPlaceSearch);

                        if (foundByProcessedContains || hasStrongWordOverlap)
                        {
                            matchingPlace = place;
                            Console.WriteLine($"DEBUG: MATCH FOUND! Place: '{matchingPlace.PlaceName}' via improved matching.");
                            break;
                        }
                    }

                    if (matchingPlace != null)
                    {
                        // isReviewRequest đã được khai báo ở trên, không cần khai báo lại
                        if (isReviewRequest)
                        {
                            latestReview = reviews
                                .Where(r => r.SnackPlaceId == matchingPlace.SnackPlaceId && r.Status && !string.IsNullOrEmpty(r.Comment))
                                .OrderByDescending(r => r.ReviewDate)
                                .FirstOrDefault();
                            Console.WriteLine($"DEBUG: Latest Review for {matchingPlace.PlaceName}: {latestReview?.Comment ?? "No review found"}");
                        }

                        dishesForPlace = allDishes
                            .Where(d => d.SnackPlaceId == matchingPlace.SnackPlaceId && d.Status)
                            .OrderByDescending(d => d.Price)
                            .Take(5)
                            .ToList();
                        Console.WriteLine($"DEBUG: Dishes for {matchingPlace.PlaceName}: {dishesForPlace.Count} found.");

                        botResponse = await CallGeminiAPIWithPlace(prompt, userName, matchingPlace, latestReview, dishesForPlace, existingMessages);
                    }
                    else if (isAskingAboutDishes) // Nếu không tìm thấy quán trực tiếp, thử tìm qua món ăn
                    {
                        foreach (var dish in allDishes)
                        {
                            var normalizedDishName = dish.Name.ToLower();
                            if ((normalizedPrompt.Contains(normalizedDishName) || RemoveDiacritics(normalizedPrompt).Contains(RemoveDiacritics(normalizedDishName))))
                            {
                                matchingPlace = snackPlaces.FirstOrDefault(p => p.SnackPlaceId == dish.SnackPlaceId);
                                if (matchingPlace != null)
                                {
                                    Console.WriteLine($"DEBUG: MATCH FOUND via DISH! Place: '{matchingPlace.PlaceName}' for dish '{dish.Name}'");
                                    // isReviewRequest đã được khai báo ở trên, không cần khai báo lại
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
                                    botResponse = await CallGeminiAPIWithPlace(prompt, userName, matchingPlace, latestReview, dishesForPlace, existingMessages);
                                    break;
                                }
                            }
                        }
                        if (matchingPlace == null)
                        {
                            Console.WriteLine("DEBUG: No specific place found via dish search. Falling back to general API.");
                            // Chuyển sang kiểm tra các từ khóa chung
                            if (_highlyRatedKeywords.Any(keyword => normalizedPrompt.Contains(keyword)))
                            {
                                var topRatedPlaces = GetTopRatedSnackPlaces(snackPlaces, reviews, 3);
                                if (topRatedPlaces.Any())
                                {
                                    botResponse = await CallGeminiAPIForTopRated(prompt, userName, topRatedPlaces, existingMessages);
                                }
                                else
                                {
                                    botResponse = "Xin lỗi, hiện tại Măm Map chưa có đủ dữ liệu đánh giá để gợi ý quán được đánh giá cao nhất. Bạn có thể thử tìm kiếm các quán theo tên hoặc khu vực nhé!";
                                }
                            }
                            else if (_feeKeywords.Any(keyword => normalizedPrompt.Contains(keyword))) // Kiểm tra phí sau khi tìm quán
                            {
                                botResponse = "Chào bạn, Măm Map hoàn toàn **miễn phí** cho người dùng tìm kiếm nhé. " +
                                              "Tụi mình chỉ thu phí đối với các **chủ quán (merchant)** với 2 gói dịch vụ là **Cơ bản** và **Tiêu chuẩn** để quản lý và quảng bá quán hiệu quả hơn. " +
                                              "Nếu bạn là chủ quán và muốn biết thêm chi tiết, hãy tải app về để tìm hiểu kỹ hơn nha!";
                            }
                            else if (isGeneralSearchOrHungry)
                            {
                                botResponse = await CallGeminiAPI(prompt, userName, snackPlaces, existingMessages);
                            }
                            else
                            {
                                botResponse = await CallGeminiAPI(prompt, userName, new List<SnackPlaces>(), existingMessages);
                            }
                        }
                    }
                    else // Nếu không tìm thấy quán bằng tên hay món ăn
                    {
                        // Chuyển sang kiểm tra các từ khóa chung
                        if (_highlyRatedKeywords.Any(keyword => normalizedPrompt.Contains(keyword)))
                        {
                            var topRatedPlaces = GetTopRatedSnackPlaces(snackPlaces, reviews, 3);
                            if (topRatedPlaces.Any())
                            {
                                botResponse = await CallGeminiAPIForTopRated(prompt, userName, topRatedPlaces, existingMessages);
                            }
                            else
                            {
                                botResponse = "Xin lỗi, hiện tại Măm Map chưa có đủ dữ liệu đánh giá để gợi ý quán được đánh giá cao nhất. Bạn có thể thử tìm kiếm các quán theo tên hoặc khu vực nhé!";
                            }
                        }
                        else if (_feeKeywords.Any(keyword => normalizedPrompt.Contains(keyword))) // Kiểm tra phí sau khi tìm quán
                        {
                            botResponse = "Chào bạn, Măm Map hoàn toàn **miễn phí** cho người dùng tìm kiếm nhé. " +
                                          "Tụi mình chỉ thu phí đối với các **chủ quán (merchant)** với 2 gói dịch vụ là **Cơ bản** và **Tiêu chuẩn** để quản lý và quảng bá quán hiệu quả hơn. " +
                                          "Nếu bạn là chủ quán và muốn biết thêm chi tiết, hãy tải app về để tìm hiểu kỹ hơn nha!";
                        }
                        else if (isGeneralSearchOrHungry)
                        {
                            botResponse = await CallGeminiAPI(prompt, userName, snackPlaces, existingMessages);
                        }
                        else
                        {
                            botResponse = await CallGeminiAPI(prompt, userName, new List<SnackPlaces>(), existingMessages);
                        }
                    }
                }

                await _chatService.AddMessageToSessionAsync(currentSessionId, "Bot", botResponse);

                return (true, "Thành công", botResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Exception in GetBotResponseAsync: {ex.Message}");
                if (currentSessionId != Guid.Empty)
                {
                    await _chatService.AddMessageToSessionAsync(currentSessionId, "Bot", "Xin lỗi, đã có lỗi xảy ra. Vui lòng thử lại sau.");
                }
                return (false, $"Lỗi xử lý: {ex.Message}", "Xin lỗi, đã có lỗi xảy ra. Vui lòng thử lại sau.");
            }
        }

        private async Task<string> CallGeminiAPIWithPlace(string prompt, string? userName, SnackPlaces place, Reviews? review, List<Dishes> allDishes, List<ChatMessage> conversationHistory)
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

            Nếu người dùng hỏi về việc 'chỉ đường' đến quán này:
            - Hãy CUNG CẤP ĐỊA CHỈ ĐẦY ĐỦ của quán.
            - Gợi ý người dùng có thể sử dụng địa chỉ này với ứng dụng bản đồ yêu thích của họ để tìm đường.

            Nếu người dùng hỏi về việc 'đánh giá' quán này:
            - Nếu có đánh giá gần nhất được cung cấp, hãy nhắc lại đánh giá đó.
            - Hướng dẫn người dùng cách tìm quán này trên ứng dụng Măm Map để họ có thể tự 'Viết đánh giá' của mình.
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

            var history = new List<object>();

            history.Add(new
            {
                role = "user",
                parts = new[] {
                    new {
                        text = "Bạn là Măm Map Bot, một trợ lý ảo của nền tảng Măm Map. Măm Map là một hệ thống giúp người dùng tìm kiếm và đánh giá các quán ăn vặt. Dựa trên thông tin dưới đây về một quán ăn, hãy trả lời một cách thân thiện, tự nhiên và hữu ích như một người từng trải nghiệm quán. Không cần liệt kê máy móc, hãy diễn đạt như một người hiểu rõ về quán. Đừng bịa thêm món ăn hoặc thông tin không có."
                    }
                }
            });
            history.Add(new
            {
                role = "model",
                parts = new[] {
                    new { text = "Vâng, tôi đã hiểu. Tôi sẽ trả lời một cách thân thiện dựa trên thông tin quán bên dưới và tuyệt đối không bịa thêm thông tin." }
                }
            });

            foreach (var msg in conversationHistory)
            {
                history.Add(new
                {
                    role = msg.Sender.ToLower() == "user" ? "user" : "model",
                    parts = new[] { new { text = msg.Text } }
                });
            }

            history.Add(new
            {
                role = "user",
                parts = new[] {
                    new {
                        text = $"Câu hỏi của người dùng: {prompt}\nThông tin quán bạn cần tư vấn:\n{infoText}"
                    }
                }
            });

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

        private async Task<string> CallGeminiAPI(string prompt, string? userName, List<SnackPlaces> snackPlaces, List<ChatMessage> conversationHistory)
        {
            var initialPrompt = @"
            Bạn là Măm Map Bot, một trợ lý ảo của nền tảng Măm Map. Măm Map là một hệ thống trung gian giúp các chủ quán ăn vặt đăng ký và quản lý thông tin quán của họ, đồng thời cho phép người dùng tìm kiếm, đánh giá và review các quán ăn vặt.

            Hãy trả lời câu hỏi của người dùng dựa trên vai trò của bạn là một trợ lý về NỀN TẢNG Măm Map.
            Bạn nên nhớ và tận dụng những thông tin người dùng đã chia sẻ trong các tin nhắn trước đó để trả lời. Ví dụ, nếu người dùng đã nói về sở thích, tên bạn bè, địa điểm cụ thể… thì hãy sử dụng lại.

            Nếu người dùng hỏi về việc 'chỉ đường đến quán' hoặc 'đánh giá quán' một cách tổng quát (không chỉ định quán cụ thể):
            - Hãy HƯỚNG DẪN người dùng cách tìm quán trên Măm Map để xem địa chỉ và sử dụng ứng dụng bản đồ khác.
            - Hoặc cách tìm quán trên Măm Map để sử dụng tính năng 'Viết đánh giá'.

            Nếu câu hỏi liên quan đến tìm quán ăn, hãy gợi ý các quán từ danh sách được cung cấp hoặc hướng dẫn cách tìm trên Măm Map.
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

            var history = new List<object>();

            history.Add(new
            {
                role = "user",
                parts = new[] {
                    new {
                        text = @"Bạn là Măm Map Bot, một trợ lý ảo của nền tảng Măm Map. Hãy trả lời câu hỏi của người dùng dựa trên vai trò của bạn.
                        Bạn nên nhớ và tận dụng những thông tin người dùng đã chia sẻ trong các tin nhắn trước đó để trả lời. Ví dụ, nếu người dùng đã nói về sở thích, tên bạn bè, địa điểm cụ thể… thì hãy sử dụng lại.
                        Nếu câu hỏi liên quan đến tìm quán ăn, hãy gợi ý các quán có sẵn hoặc hướng dẫn cách tìm trên Măm Map. Luôn thân thiện và hữu ích."
                    }
                }
            });
            history.Add(new
            {
                role = "model",
                parts = new[] {
                    new { text = "Vâng, tôi đã hiểu rõ. Măm Map xin chào! Tôi là Măm Map Bot, sẵn sàng hỗ trợ bạn về cách sử dụng nền tảng review quán ăn vặt Măm Map. Bạn có câu hỏi nào cho tôi không?" }
                }
            });

            foreach (var msg in conversationHistory)
            {
                history.Add(new
                {
                    role = msg.Sender.ToLower() == "user" ? "user" : "model",
                    parts = new[] { new { text = msg.Text } }
                });
            }

            history.Add(new
            {
                role = "user",
                parts = new[] { new { text = fullPrompt.ToString() } }
            });

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

        private async Task<string> CallGeminiAPIForTopRated(string prompt, string? userName, List<(SnackPlaces place, double averageRating)> topRatedPlaces, List<ChatMessage> conversationHistory)
        {
            var promptInstruction = @"
            Bạn là Măm Map Bot, một trợ lý ảo chuyên tư vấn quán ăn vặt.
            Mục tiêu của bạn là giới thiệu các quán ăn vặt được đánh giá cao nhất trên Măm Map một cách thân thiện, tự nhiên và nhiệt tình.
            Hãy giới thiệu những quán này như một người bạn đang gợi ý địa điểm ăn uống chất lượng.
            Đừng bịa thêm thông tin quán hay đánh giá không có.
            Nếu có đánh giá gần nhất, hãy lồng ghép nó vào câu trả lời một cách tự nhiên.
            ";

            var infoText = new StringBuilder();
            infoText.AppendLine("Dưới đây là danh sách các quán ăn vặt được đánh giá cao nhất trên Măm Map:");
            foreach (var item in topRatedPlaces)
            {
                infoText.AppendLine($"Tên quán: {item.place.PlaceName}");
                infoText.AppendLine($"Địa chỉ: {item.place.Address}");
                infoText.AppendLine($"Đánh giá trung bình: {item.averageRating:F1}/5 sao");
                if (!string.IsNullOrEmpty(item.place.Description))
                    infoText.AppendLine($"Mô tả: {item.place.Description}");
                infoText.AppendLine("---");
            }

            infoText.Insert(0, promptInstruction + "\n\n");

            var history = new List<object>();

            history.Add(new
            {
                role = "user",
                parts = new[] {
                    new {
                        text = "Bạn là Măm Map Bot, một trợ lý ảo của nền tảng Măm Map. Măm Map là một hệ thống giúp người dùng tìm kiếm và đánh giá các quán ăn vặt. Dựa trên thông tin dưới đây về các quán ăn được đánh giá cao, hãy trả lời một cách thân thiện, tự nhiên và hữu ích như một người từng trải nghiệm quán. Không cần liệt kê máy móc, hãy diễn đạt như một người hiểu rõ về quán. Đừng bịa thêm thông tin không có."
                    }
                }
            });
            history.Add(new
            {
                role = "model",
                parts = new[] {
                    new { text = "Vâng, tôi đã hiểu. Tôi sẽ trả lời một cách thân thiện dựa trên thông tin quán được đánh giá cao bên dưới và tuyệt đối không bịa thêm thông tin." }
                }
            });

            foreach (var msg in conversationHistory)
            {
                history.Add(new
                {
                    role = msg.Sender.ToLower() == "user" ? "user" : "model",
                    parts = new[] { new { text = msg.Text } }
                });
            }

            history.Add(new
            {
                role = "user",
                parts = new[] {
                    new {
                        text = $"Câu hỏi của người dùng: {prompt}\nThông tin các quán được đánh giá cao:\n{infoText}"
                    }
                }
            });

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
            Console.WriteLine($"DEBUG: ExtractKeywords - Input: '{input}'");
            var words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(word => word.ToLower());

            var filteredWords = words.Where(word => !_noiseWords.Contains(word));

            var result = string.Join(" ", filteredWords).Trim();
            Console.WriteLine($"DEBUG: ExtractKeywords - Output: '{result}'");
            return result;
        }

        private static string RemoveDiacritics(string input)
        {
            Console.WriteLine($"DEBUG: RemoveDiacritics - Input: '{input}'");
            var normalized = input.Normalize(NormalizationForm.FormD);
            var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
            var result = new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
            Console.WriteLine($"DEBUG: RemoveDiacritics - Output: '{result}'");
            return result;
        }

        private List<(SnackPlaces place, double averageRating)> GetTopRatedSnackPlaces(List<SnackPlaces> snackPlaces, List<Reviews> reviews, int count)
        {
            var placeRatings = new Dictionary<Guid, List<double>>();

            foreach (var review in reviews.Where(r => r.Status))
            {
                var ratings = new List<int>();

                if (review.TasteRating > 0) ratings.Add(review.TasteRating);
                if (review.PriceRating > 0) ratings.Add(review.PriceRating);
                if (review.SanitaryRating > 0) ratings.Add(review.SanitaryRating);
                if (review.TextureRating > 0) ratings.Add(review.TextureRating);
                if (review.ConvenienceRating > 0) ratings.Add(review.ConvenienceRating);

                if (ratings.Any())
                {
                    double reviewAverage = ratings.Average();
                    if (!placeRatings.ContainsKey(review.SnackPlaceId))
                    {
                        placeRatings[review.SnackPlaceId] = new List<double>();
                    }
                    placeRatings[review.SnackPlaceId].Add(reviewAverage);
                }
            }

            var topRated = placeRatings
                .Select(pr => new
                {
                    SnackPlaceId = pr.Key,
                    AverageRating = pr.Value.Average()
                })
                .OrderByDescending(pr => pr.AverageRating)
                .Join(snackPlaces,
                      pr => pr.SnackPlaceId,
                      sp => sp.SnackPlaceId,
                      (pr, sp) => (place: sp, averageRating: pr.AverageRating))
                .Take(count)
                .ToList();

            return topRated;
        }
    }
}