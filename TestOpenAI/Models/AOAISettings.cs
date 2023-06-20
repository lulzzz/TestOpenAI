namespace TestOpenAI.Models
{
    public class AOAISettings
    {
        public string? AOAI_ENDPOINT { get; set; }
        public string? AOAI_KEY { get; set; }

        
        public string? AOAI_TEXT_DEPLOYMENT_NAME { get; set; } //e.g. jl-text-davinci-003
        public string? AOAI_TEXT_DEPLOYMENT_MODEL { get; set; } //e.g. text-davinci-003


        public string? AOAI_CHAT_DEPLOYMENT_NAME { get; set; } //e.g. jl-gpt-35-turbo
        public string? AOAI_CHAT_DEPLOYMENT_MODEL { get; set; } //e.g. gpt-35-turbo

        public string? AOAI_EMBEDDED_DEPLOYMENT_NAME { get; set; }
        public string? AOAI_EMBEDDED_DEPLOYMENT_MODEL { get; set; }


    }
}
