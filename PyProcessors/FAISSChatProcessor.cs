﻿using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        Py.GILState GILState;
        PyModule scope;
        public FAISSChatProcessor(string apiKey, string azureApiEndpoint, string chatDeploymentName, string chatModelName,  string azureApiVersion = "2023-05-15") 
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
            GILState = Py.GIL();
            scope = Py.CreateScope();
        }
        public dynamic GetChatModule(string faissIndexModelFilePath)
        {
            dynamic vectorstores = scope.Import("langchain.vectorstores");
            dynamic chains = scope.Import("langchain.chains");
            dynamic question_answering = scope.Import("langchain.chains.question_answering");
            dynamic chat_models = scope.Import("langchain.chat_models");
            dynamic openApiLangChain = scope.Import("langchain.embeddings.openai");

            dynamic llm = chat_models.AzureChatOpenAI(
                Py.kw("deployment_name", OPENAI_DEPLOYMENT_NAME),
                Py.kw("model_name", OPENAI_MODEL_NAME),
                Py.kw("openai_api_base", OPENAI_DEPLOYMENT_ENDPOINT),
                Py.kw("openai_api_version", OPENAI_DEPLOYMENT_VERSION),
                Py.kw("openai_api_key", OPENAI_API_KEY)
                );
            dynamic embeddings = openApiLangChain.OpenAIEmbeddings(
                Py.kw("model", OPENAI_EMBEDDING_MODEL_NAME),
                Py.kw("chunk_size", 1)
            );

            //load the faiss vector store we saved into memory
            dynamic vectorStore = vectorstores.FAISS.load_local(faissIndexModelFilePath, embeddings);

            //use the faiss vector store we saved to search the local document
            dynamic retriever = vectorStore.as_retriever(
                Py.kw("search_type", "similarity"),
                Py.kw("search_kwargs", "{ \"k\":2}")
                );
            //use the vector store as a retriever
            dynamic qa = chains.RetrievalQA.from_chain_type(
                Py.kw("llm", llm),
                Py.kw("chain_type", "stuff"),
                Py.kw("retriever", retriever),
                Py.kw("return_source_documents", false.ToPython())
                );
            return qa;
        }
        public dynamic AskQuestion(dynamic qaChat, string question)
        {
            //var result = qaChat({ "query": question});
            var result = qaChat(Py.kw("query", question));
            return result["result"];
        }
        public void Dispose()
        {
            scope?.Dispose();
            GILState?.Dispose();
        }
    }
}