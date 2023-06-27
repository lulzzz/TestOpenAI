using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace PyProcessors
{
    public class FAISSChatProcessor : IDisposable
    {
        string OPENAI_API_KEY = String.Empty;
        string OPENAI_DEPLOYMENT_ENDPOINT = String.Empty;
        string OPENAI_DEPLOYMENT_NAME = String.Empty;
        string OPENAI_EMBEDDING_MODEL_NAME = String.Empty;
        string OPENAI_DEPLOYMENT_VERSION = String.Empty;
        string OPENAI_MODEL_NAME = String.Empty;

        //Py.GILState GILState;
        //PyModule scope;

        public EventHandler<string> OnProcessStarted { get; set; }
        public EventHandler<bool> OnProcessCompleted { get; set; }
        public FAISSChatProcessor(string? apiKey, string? azureApiEndpoint, string? chatDeploymentName, string? chatModelName, string? embeddingModelName,  string azureApiVersion = "2023-05-15") 
        {
            if (String.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));
            if (String.IsNullOrWhiteSpace(azureApiEndpoint)) throw new ArgumentNullException(nameof(azureApiEndpoint));
            if (String.IsNullOrWhiteSpace(azureApiVersion)) throw new ArgumentNullException(nameof(azureApiVersion));
            if (String.IsNullOrWhiteSpace(chatDeploymentName)) throw new ArgumentNullException(nameof(chatDeploymentName));
            if(String.IsNullOrWhiteSpace(chatModelName)) throw new ArgumentNullException(nameof(chatModelName));

            OPENAI_API_KEY = apiKey;
            OPENAI_DEPLOYMENT_ENDPOINT = azureApiEndpoint;
            OPENAI_DEPLOYMENT_VERSION = azureApiVersion;
            OPENAI_DEPLOYMENT_NAME=chatDeploymentName;
            OPENAI_MODEL_NAME = chatModelName;
            OPENAI_EMBEDDING_MODEL_NAME = embeddingModelName;
            //GILState = Py.GIL();
            //scope = Py.CreateScope();
        }
        public dynamic GetChatModule(string faissIndexModelFilePath)
        {
            //OnProcessStarted?.Invoke(this, "Loading modules");
            dynamic qa = null;
            var success = false;
            using (Py.GIL())
            {
                using(var scope = Py.CreateScope())
                {
                    try
                    {
                        //OnProcessStarted?.Invoke(this, "Loading modules");
                        dynamic vectorstores = scope.Import("langchain.vectorstores");
                        dynamic chains = scope.Import("langchain.chains");
                        dynamic question_answering = scope.Import("langchain.chains.question_answering");
                        dynamic chat_models = scope.Import("langchain.chat_models");
                        dynamic openApiLangChain = scope.Import("langchain.embeddings.openai");
                        dynamic openai = scope.Import("openai");

                        openai.api_type = "azure";
                        openai.api_version = OPENAI_DEPLOYMENT_VERSION;
                        openai.api_base = OPENAI_DEPLOYMENT_ENDPOINT;
                        openai.api_key = OPENAI_API_KEY;

                        dynamic llm = chat_models.AzureChatOpenAI(
                            Py.kw("deployment_name", OPENAI_DEPLOYMENT_NAME),
                            Py.kw("model_name", OPENAI_MODEL_NAME),
                            Py.kw("openai_api_base", OPENAI_DEPLOYMENT_ENDPOINT),
                            Py.kw("openai_api_version", OPENAI_DEPLOYMENT_VERSION),
                            Py.kw("openai_api_key", OPENAI_API_KEY)
                            );
                        dynamic embeddings = openApiLangChain.OpenAIEmbeddings(
                            Py.kw("model", OPENAI_EMBEDDING_MODEL_NAME),
                            Py.kw("deployment", OPENAI_EMBEDDING_MODEL_NAME),
                            Py.kw("chunk_size", 1),
                            Py.kw("openai_api_key", OPENAI_API_KEY)
                        );

                        //load the faiss vector store we saved into memory
                        dynamic vectorStore = vectorstores.FAISS.load_local(faissIndexModelFilePath, embeddings);

                        //use the faiss vector store we saved to search the local document
                        dynamic retriever = vectorStore.as_retriever(
                            Py.kw("search_type", "similarity"),
                            //Py.kw("search_kwargs", "{ \"k\":2}")
                            Py.kw("search_kwargs",Py.kw("k",2))
                            );
                        //use the vector store as a retriever
                        qa = chains.RetrievalQA.from_chain_type(
                            Py.kw("llm", llm),
                            Py.kw("chain_type", "stuff"),
                            //Py.kw("chain_type","refine"),
                            Py.kw("retriever", retriever),
                            Py.kw("return_source_documents", false.ToPython())
                            );
                        //OnProcessCompleted?.Invoke(this, true);
                        success = true;
                        //return qa;
                    }
                    catch (Exception ex)
                    {
                        //OnProcessCompleted?.Invoke(this, false);
                        //throw;
                        success = false;
                        //return null;
                    }
                }
            }
            //OnProcessCompleted?.Invoke(this, success);
            return qa;
        }
        
        public string? AskQuestion(dynamic qaChat, string question)
        {
            using (Py.GIL())
            {
                using var scope = Py.CreateScope();
                dynamic openai = scope.Import("openai");
                openai.api_type = "azure";
                openai.api_version = OPENAI_DEPLOYMENT_VERSION;
                openai.api_base = OPENAI_DEPLOYMENT_ENDPOINT;
                openai.api_key = OPENAI_API_KEY;
                //var result = qaChat({ "query": question});
                try
                {
                    OnProcessStarted?.Invoke(this, "Thinking...");
                    //var result = qaChat(Py.kw("query", question));
                    dynamic result = qaChat(Py.kw("inputs", Py.kw("query", question)));
                    //return result;
                    var answer = result["result"];
                    var pyString = new PyString(answer);
                    
                    //return result;
                    OnProcessCompleted?.Invoke(this, true);
                    return pyString.ToString();
                }
                catch (Exception ex)
                {
                    OnProcessCompleted?.Invoke(this, false);
                    throw;
                }
                
            }
                
        }
        public dynamic GetConversationModule(string faissIndexModelPath)
        {
            dynamic qa = null;
            using (Py.GIL())
            {
                using var scope = Py.CreateScope();
                try
                {
                    dynamic vectorstores = scope.Import("langchain.vectorstores");
                    dynamic chains = scope.Import("langchain.chains");
                    dynamic chat_models = scope.Import("langchain.chat_models");
                    dynamic openApiLangChain = scope.Import("langchain.embeddings.openai");
                    dynamic openai = scope.Import("openai");
                    dynamic prompts = scope.Import("langchain.prompts");
                    dynamic sumMemory = scope.Import("langchain.chains.conversation.memory");


                    openai.api_type = "azure";
                    openai.api_version = OPENAI_DEPLOYMENT_VERSION;
                    openai.api_base = OPENAI_DEPLOYMENT_ENDPOINT;
                    openai.api_key = OPENAI_API_KEY;

                    dynamic llm = chat_models.AzureChatOpenAI(
                        Py.kw("deployment_name", OPENAI_DEPLOYMENT_NAME),
                        Py.kw("model_name", OPENAI_MODEL_NAME),
                        Py.kw("openai_api_base", OPENAI_DEPLOYMENT_ENDPOINT),
                        Py.kw("openai_api_version", OPENAI_DEPLOYMENT_VERSION),
                        Py.kw("openai_api_key", OPENAI_API_KEY)
                        );
                    dynamic embeddings = openApiLangChain.OpenAIEmbeddings(
                        Py.kw("model", OPENAI_EMBEDDING_MODEL_NAME),
                        Py.kw("deployment", OPENAI_EMBEDDING_MODEL_NAME),
                        Py.kw("chunk_size", 1),
                        Py.kw("openai_api_key", OPENAI_API_KEY)
                    );

                    //load the faiss vector store we saved into memory
                    dynamic vectorStore = vectorstores.FAISS.load_local(faissIndexModelPath, embeddings);

                    //use the faiss vector store we saved to search the local document
                    dynamic retriever = vectorStore.as_retriever();

                    var promptTemplate = @"""Given the following conversation and a follow up question, rephrase the follow up question to be a standalone question.

                            Chat History:
                            {chat_history}
                            Follow Up Input: {question}
                            Standalone question:""";
                    dynamic CONDENSE_QUESTION_PROMPT = prompts.PromptTemplate.from_template(promptTemplate);
                    var bufferSumMemory = sumMemory.ConversationSummaryMemory(Py.kw("llm", llm), Py.kw("memory_key", "chat_history"), Py.kw("return_messages", true.ToPython()));
                    qa = chains.ConversationalRetrievalChain.from_llm(
                        Py.kw("llm", llm),
                        Py.kw("retriever", retriever),
                        Py.kw("condense_question_prompt", CONDENSE_QUESTION_PROMPT),
                        Py.kw("return_source_documents", false.ToPython()),
                        Py.kw("verbose", false.ToPython()),
                        Py.kw("memory", bufferSumMemory)
                        );

                }
                catch (Exception ex)
                {

                    //throw;
                }
                return qa;
            }
        }
        public dynamic AskConversationQuestion(dynamic qaConversation, string question, string? lastQuestion = null, string? lastAnswer = null)
        {
            using (Py.GIL())
            {
                using var scope = Py.CreateScope();
                dynamic openai = scope.Import("openai");
                openai.api_type = "azure";
                openai.api_version = OPENAI_DEPLOYMENT_VERSION;
                openai.api_base = OPENAI_DEPLOYMENT_ENDPOINT;
                openai.api_key = OPENAI_API_KEY;
                //var result = qaChat({ "query": question});
                try
                {
                    OnProcessStarted?.Invoke(this, "Thinking...");

                    
                    dynamic result = qaConversation(Py.kw("inputs", Py.kw("question", question)));

                    
                    var answer = result["answer"];
                    var pyString = new PyString(answer);
                   
                    OnProcessCompleted?.Invoke(this, true);
                    return pyString.ToString();
                }
                catch (Exception ex)
                {
                    OnProcessCompleted?.Invoke(this, false);
                    throw;
                }
            }
                
        }
        public void Dispose()
        {
            //scope?.Dispose();
            //GILState?.Dispose();
        }
    }
}
