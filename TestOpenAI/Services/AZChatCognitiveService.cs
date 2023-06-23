using Azure.AI.OpenAI;

namespace TestOpenAI.Services
{
    public class AZChatCognitiveService
    {
        private readonly HttpClient _httpClient;
        private string _chatGptEndpoint;
        private string _chatGptDeploymentId;
        private string _chatGptKey;
        private string _aoaiVersion;
        //private string _chatGptVersion;
        private string _cognitiveSearchEndpoint;
        private string _cognitiveSearchAdminKey;
        private string _cognitiveSearchIndexName;

        public List<ChatMessage> ChatMessages { get; set; } = new();
        public AZChatCognitiveService(IHttpClientFactory httpClientFactory)
        {
            this._httpClient = httpClientFactory.CreateClient();
        }
        public void Initialize(
            string aoaiEndpoint, string aoaiChatDeploymentName, string aoaiKey, string aoaiVersion,
            string cognitiveSearchEndpoint, string cognitiveSearchAdminKey, string cognitiveSearchIndexName
        )
        {
            this._chatGptEndpoint = aoaiEndpoint;
            this._chatGptDeploymentId = aoaiChatDeploymentName;
            this._chatGptKey = aoaiKey;
            this._aoaiVersion = aoaiVersion;
            //this._chatGptVersion = chatGptVersion;
            this._cognitiveSearchEndpoint = cognitiveSearchEndpoint;
            this._cognitiveSearchAdminKey = cognitiveSearchAdminKey;
            this._cognitiveSearchIndexName = cognitiveSearchIndexName;

            this.SetupHttpClient();
            
        }

        private void SetupHttpClient()
        {
            var chatGptUrl = $"{_chatGptEndpoint.TrimEnd('/')}/openai/deployments/{_chatGptDeploymentId}/chat/completions?api-version={_aoaiVersion}";
            _httpClient.DefaultRequestHeaders.Add("api-key", _chatGptKey);
            _httpClient.DefaultRequestHeaders.Add("chatgpt_url", chatGptUrl);
            _httpClient.DefaultRequestHeaders.Add("chatgpt_key", _chatGptKey);
        }
        private RequestBody GetRequestBody()
        {
            var body = new RequestBody()
            {
                temperature = 0f,
                max_tokens = 1000,
                top_p = 1f,
                stream = false,

            };
            body.dataSources.Add(new AZCognitiveSearchDataSource()
            {
                parameters = new Parameters()
                {
                    endpoint = _cognitiveSearchEndpoint,
                    key = _cognitiveSearchAdminKey,
                    indexName = _cognitiveSearchIndexName,
                    inScope = true,
                    topNDocuments = 5,
                    queryType = "simple",
                    roleInformation = "I am your helper for employees"
                }
            });
            foreach (var chatMessage in ChatMessages.TakeLast(3))
            {
                body.messages.Add(new Message() { role = chatMessage.Role.Label, content = chatMessage.Content});
            }
            return body;
        }

        public async Task DoChat(string message)
        {
            var url = $"{_chatGptEndpoint.TrimEnd('/')}/openai/deployments/{_chatGptDeploymentId}/extensions/chat/completions?api-version={_aoaiVersion}";
            ChatMessages.Add(new ChatMessage(ChatRole.User, message));
            var body = GetRequestBody();

            var resp = await _httpClient.PostAsJsonAsync<RequestBody>(url, body);
            if (resp.IsSuccessStatusCode)
            {
                //var ret = await resp.Content.ReadFromJsonAsync<ChatCompletions>();
                //var ret = await resp.Content.ReadAsStringAsync();
                var ret = await resp.Content.ReadFromJsonAsync<CognitiveResponse>();

                foreach (var choice in ret.choices)
                {
                    foreach (var msg in choice.messages.Where(x=>x.role == ChatRole.Assistant.Label).OrderBy(x=>x.index))
                    {
                        ChatMessages.Add(new ChatMessage(ChatRole.Assistant, msg.content));
                    }
                }
            }
        }
    }
    #region request
    class RequestBody //: ChatCompletionsOptions
    {
        public List<Message> messages { get; set; } = new();
        public float temperature { get; set; } = 0f;
        public int max_tokens { get; set; }
        public float top_p { get; set; } = 1f;
        public string[]? stop { get; set; } = null;
        public bool stream { get; set; }

        public List<AZCognitiveSearchDataSource> dataSources { get; set; } = new();
    }
    class Message
    {
        public string role { get; set; }
        public string content { get; set; }
    }
    class AZCognitiveSearchDataSource
    {
        public string type { get; set; } = "AzureCognitiveSearch";
        public Parameters parameters { get; set; }

    }
    class Parameters
    {
        public string endpoint { get; set; }
        /// <summary>
        /// cognitive admin key
        /// </summary>
        public string key { get; set; }
        public string indexName { get; set; }
        public FieldsMapping? fieldsMapping { get; set; }
        public int? topNDocuments { get; set; }
        /// <summary>
        /// "simple", "semantic" or "full"
        /// </summary>
        public string queryType { get; set; } = "simple";
        public string? semanticConfiguration { get; set; }
        public bool inScope { get; set; }
        public string? roleInformation { get; set; }
    }
    class FieldsMapping
    {
        public string titleField { get; set; }
        public string urlField { get; set; }
        public string filepathField { get; set; }
        public List<string> contentFields { get; set; }
        public string contentFieldsSeparator { get; set; }
    }
    #endregion

    #region response
    public class ResponseChoice
    {
        public int index { get; set; }
        public List<ResponseMessage> messages { get; set; } = new();
    }

    public class ResponseMessage
    {
        public int index { get; set; }
        public string? role { get; set; }
        public bool end_turn { get; set; }
        public string? content { get; set; }
    }

    public class CognitiveResponse
    {
        public string? id { get; set; }
        public string? model { get; set; }
        public int created { get; set; }
        public string? @object { get; set; }
        public List<ResponseChoice> choices { get; set; } = new();
    }
    #endregion

}
