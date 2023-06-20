using PyProcessors.Helpers;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PyProcessors
{
    public class PdfProcessor
    {
        string OPENAI_API_KEY = String.Empty;
        string OPENAI_DEPLOYMENT_ENDPOINT = String.Empty;
        string OPENAI_EMBEDDING_MODEL_NAME = String.Empty;
        string OPENAI_DEPLOYMENT_VERSION = String.Empty;

        public EventHandler<string> OnProcess { get; set; }
        public EventHandler<bool> OnCompleted { get; set; }
        public PdfProcessor(string? apiKey, string? azureApiEndpoint, string? azureApiEmbeddingDeploymentName, string? azureApiVersion = "2023-05-15") 
        {
            if(String.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));
            if (String.IsNullOrWhiteSpace(azureApiEndpoint)) throw new ArgumentNullException(nameof(azureApiEndpoint));
            if (String.IsNullOrWhiteSpace(azureApiVersion)) throw new ArgumentNullException(nameof(azureApiVersion));
            if (String.IsNullOrWhiteSpace(azureApiEmbeddingDeploymentName)) throw new ArgumentNullException(nameof(azureApiEmbeddingDeploymentName));

            OPENAI_API_KEY = apiKey;
            OPENAI_DEPLOYMENT_ENDPOINT = azureApiEndpoint;
            OPENAI_DEPLOYMENT_VERSION = azureApiVersion;
            OPENAI_EMBEDDING_MODEL_NAME = azureApiEmbeddingDeploymentName;

        }


        public string ProcessPdf(string fileName, string dirIndex)
        {
            try
            {
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

                //var fileIndex = Path.Combine(dirIndex, Path.ChangeExtension(Path.GetFileName(fileName), ".index"));
                //var fileIndex = Path.Combine(dirIndex, Path.GetFileNameWithoutExtension(fileName));
                var fileIndex = Path.Combine(dirIndex, Path.GetFileNameWithoutExtension(fileName).CleanString());
                if(!Directory.Exists(fileIndex))
                {
                    Directory.CreateDirectory(fileIndex);
                }
                //if (!File.Exists(fileIndex))
                if(!Directory.Exists(fileIndex) || !Directory.GetFiles(fileIndex).Any())
                {
                    using var _ = Py.GIL();
                    using var scope = Py.CreateScope();

                    OnProcess?.Invoke(this, "Importing Python libraries");

                    dynamic pyPdf = scope.Import("langchain.document_loaders.pdf");
                    dynamic openApiLangChain = scope.Import("langchain.embeddings.openai");
                    dynamic openai = scope.Import("openai");
                    dynamic vectorchains = scope.Import("langchain.vectorstores");

                    OnProcess?.Invoke(this, "Initializing AI");
                    //initialize azure openai
                    openai.api_type = "azure";
                    openai.api_version = OPENAI_DEPLOYMENT_VERSION;
                    openai.api_base = OPENAI_DEPLOYMENT_ENDPOINT;
                    openai.api_key = OPENAI_API_KEY;

                    OnProcess?.Invoke(this, "Creating embeddings");
                    dynamic embeddings = openApiLangChain.OpenAIEmbeddings(
                        Py.kw("model", OPENAI_EMBEDDING_MODEL_NAME),
                        Py.kw("chunk_size", 1),
                        Py.kw("openai_api_key", OPENAI_API_KEY)
                        );
                    dynamic loader = pyPdf.PyPDFLoader(fileName);

                    OnProcess?.Invoke(this, "Splitting document to pages");
                    var pages = loader.load_and_split();//

                    OnProcess?.Invoke(this, "Indexing document");
                    dynamic db = vectorchains.FAISS.from_documents(Py.kw("documents", pages), Py.kw("embedding", embeddings));

                    OnProcess?.Invoke(this, "Saving index");
                    db.save_local(fileIndex);
                }
                else
                    OnProcess?.Invoke(this, "Index already exists for this file.");
                OnCompleted?.Invoke(this, true);
                return Path.GetFileName(fileIndex);
            }
            catch (Exception ex)
            {
                OnCompleted?.Invoke(this, false);
                throw;
            }
            
        }
    }
}
