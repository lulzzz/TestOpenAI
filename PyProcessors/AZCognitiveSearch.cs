using Python.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PyProcessors
{
    public class AZCognitiveSearch
    {
        public EventHandler<string>? OnProcessStarted { get; set; }
        public EventHandler<bool>? OnProcessCompleted { get; set; }


        string OPENAI_API_KEY = String.Empty;
        string OPENAI_DEPLOYMENT_ENDPOINT = String.Empty;
        string OPENAI_DEPLOYMENT_NAME = String.Empty;
        string OPENAI_EMBEDDING_MODEL_NAME = String.Empty;
        string OPENAI_DEPLOYMENT_VERSION = String.Empty;
        string OPENAI_MODEL_NAME = String.Empty;


        public AZCognitiveSearch(string? apiKey, string? azureApiEndpoint, string? chatDeploymentName, string? chatModelName, string? embeddingModelName,
            string azureApiVersion = "2023-05-15") 
        {
            if (String.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));
            if (String.IsNullOrWhiteSpace(azureApiEndpoint)) throw new ArgumentNullException(nameof(azureApiEndpoint));
            if (String.IsNullOrWhiteSpace(azureApiVersion)) throw new ArgumentNullException(nameof(azureApiVersion));
            if (String.IsNullOrWhiteSpace(chatDeploymentName)) throw new ArgumentNullException(nameof(chatDeploymentName));
            if (String.IsNullOrWhiteSpace(chatModelName)) throw new ArgumentNullException(nameof(chatModelName));

            

            OPENAI_API_KEY = apiKey;
            OPENAI_DEPLOYMENT_ENDPOINT = azureApiEndpoint;
            OPENAI_DEPLOYMENT_VERSION = azureApiVersion;
            OPENAI_DEPLOYMENT_NAME = chatDeploymentName;
            OPENAI_MODEL_NAME = chatModelName;
            OPENAI_EMBEDDING_MODEL_NAME = embeddingModelName;

        }
        public dynamic GetChatModule(string azSearchEndpoint, string azSearchQueryKey, string azSearchIndexName)
        {
            if (String.IsNullOrWhiteSpace(azSearchEndpoint)) throw new ArgumentNullException(nameof(azSearchEndpoint));
            if (String.IsNullOrWhiteSpace(azSearchQueryKey)) throw new ArgumentNullException(nameof(azSearchQueryKey));
            if (String.IsNullOrWhiteSpace(azSearchIndexName)) throw new ArgumentNullException(nameof(azSearchIndexName));

            dynamic vectorStore = null;
            var success = false;
            using (Py.GIL())
            {
                using (var scope = Py.CreateScope())
                {
                    try
                    {
                        //OnProcessStarted?.Invoke(this, "Loading modules");
                        dynamic azureSearch = scope.Import("langchain.vectorstores.azuresearch");
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
                        vectorStore = azureSearch.AzureSearch(
                            Py.kw("azure_search_endpoint", azSearchEndpoint),
                            Py.kw("azure_search_key", azSearchQueryKey),
                            Py.kw("index_name", azSearchIndexName),
                            Py.kw("embedding_function", embeddings.embed_query)
                            );

                        ////use the faiss vector store we saved to search the local document
                        //dynamic retriever = vectorStore.as_retriever(
                        //    Py.kw("search_type", "similarity"),
                        //    //Py.kw("search_kwargs", "{ \"k\":2}")
                        //    Py.kw("search_kwargs", Py.kw("k", 2))
                        //    );
                        ////use the vector store as a retriever
                        //qa = chains.RetrievalQA.from_chain_type(
                        //    Py.kw("llm", llm),
                        //    Py.kw("chain_type", "stuff"),
                        //    //Py.kw("chain_type","refine"),
                        //    Py.kw("retriever", retriever),
                        //    Py.kw("return_source_documents", true.ToPython())
                        //    );
                        ////OnProcessCompleted?.Invoke(this, true);
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
            return vectorStore;
        }
        public string? AskQuestion(dynamic vectorStore, string question)
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
                    var docs = vectorStore.similarity_search(
                        Py.kw("query", question),
                        Py.kw("k", 3)
                    );
                    //return result;
                    OnProcessCompleted?.Invoke(this, true);
                    return new PyString(docs[0].page_content).ToString();
                }
                catch (Exception ex)
                {
                    OnProcessCompleted?.Invoke(this, false);
                    throw;
                }

            }

        }
    }
}
