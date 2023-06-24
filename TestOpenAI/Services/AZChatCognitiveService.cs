using Azure.AI.OpenAI;
using System.Text;
using System.Text.RegularExpressions;

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

        public Dictionary<ChatMessage, List<ResponseCitation>> ChatMessagesWithCitations { get; set; } = new();
        public List<ResponseCitation> Citations { get => ChatMessagesWithCitations.SelectMany(x=>x.Value).ToList(); }

        public EventHandler<bool> OnBusy { get; set; }

        //public List<ChatMessage> ChatMessages { get; set; } = new();
        //public List<ResponseCitation> Citations{get;set;} = new();
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
            foreach (var chatMessage in ChatMessagesWithCitations.TakeLast(3).Select(x=>x.Key)) // ChatMessages.TakeLast(3))
            {
                body.messages.Add(new Message() { role = chatMessage.Role.Label, content = chatMessage.Content});
            }
            return body;
        }

        public async Task DoChat(string message)
        {
            OnBusy.Invoke(this, true);
            var url = $"{_chatGptEndpoint.TrimEnd('/')}/openai/deployments/{_chatGptDeploymentId}/extensions/chat/completions?api-version={_aoaiVersion}";
            //ChatMessages.Add(new ChatMessage(ChatRole.User, message));
            ChatMessagesWithCitations.Add(new ChatMessage(ChatRole.User, message), new List<ResponseCitation>());
            var body = GetRequestBody();

            var resp = await _httpClient.PostAsJsonAsync<RequestBody>(url, body);
            if (resp.IsSuccessStatusCode)
            {
                //var ret = await resp.Content.ReadFromJsonAsync<ChatCompletions>();
                //var ret = await resp.Content.ReadAsStringAsync();
                var ret = await resp.Content.ReadFromJsonAsync<CognitiveResponse>();
                
                foreach (var choice in ret.choices)
                {
                    List<ChatMessage> choiceMessages = new();
                    List<ResponseCitation> choiceCitations = new();
                    //foreach (var msg in choice.messages.Where(x=>x.role == ChatRole.Assistant.Label).OrderBy(x=>x.index))
                    //{
                    //    choiceMessages.Add(new ChatMessage(ChatRole.Assistant, msg.content));
                    //}
                    foreach (var msg in choice.messages.Where(s=>s.role == "tool"))
                    {
                        try
                        {
                            var citations = System.Text.Json.JsonSerializer.Deserialize<ResponseCitations>(msg.content);
                            foreach (var citation in citations.citations)
                            {
                                citation.messageId = ret.id;
                            }
                            choiceCitations.AddRange(citations.citations);
                        }
                        catch (Exception ex)
                        {

                            //throw;
                        }
                        
                    }
                    foreach (var msg in choice.messages.Where(x => x.role == ChatRole.Assistant.Label).OrderBy(x => x.index))
                    {
                        var msgContent = msg.content;
                        foreach (var citation in choiceCitations)
                        {
                            int docIndex = choiceCitations.IndexOf(citation) + 1;// int.Parse(citation.chunk_id) + 1;
                            var strToSearch = $"[doc{docIndex}]";
                            var strToReplaceWith = "";// $"<sup>{docIndex}</sup>";
                            msgContent = msgContent.Replace(strToSearch, strToReplaceWith);
                        }
                        //ChatMessages.Add(new ChatMessage(msg.Role, msgContent));
                        ChatMessagesWithCitations.Add(new ChatMessage(ChatRole.Assistant, msgContent), choiceCitations);
                    }

                    
                }
            }
            OnBusy.Invoke(this, false);
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
    class ResponseChoice
    {
        public int index { get; set; }
        public List<ResponseMessage> messages { get; set; } = new();
    }

    class ResponseMessage
    {
        public int index { get; set; }
        public string? role { get; set; }
        public bool end_turn { get; set; }
        public string? content { get; set; }
    }

    class CognitiveResponse
    {
        public string? id { get; set; }
        public string? model { get; set; }
        public int created { get; set; }
        public string? @object { get; set; }
        public List<ResponseChoice> choices { get; set; } = new();
    }

    public class ResponseCitation
    {
        public string? messageId { get; set; }
        public string? content { get; set; }
        public string? id { get; set; }
        public string? title { get; set; }
        public string? filepath { get; set; }
        public string? url { get; set; }
        public string? chunk_id { get; set; }

        public string? uniqueid { get => $"{messageId}_{chunk_id}"; }
    }
    class ResponseCitations
    {
        public List<ResponseCitation> citations { get; set; } = new();
    }
    #endregion

}
