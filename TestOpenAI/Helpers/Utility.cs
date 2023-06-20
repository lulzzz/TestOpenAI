using Microsoft.AspNetCore.Hosting;

namespace TestOpenAI.Helpers
{
    public static class Utility
    {
        public static string GetDocumentSourceFolder(IWebHostEnvironment webHostEnvironment)
        {
            var folder = Path.Combine(webHostEnvironment.ContentRootPath, "dbs", "source");
            if (!Directory.Exists(folder)) { Directory.CreateDirectory(folder); }
            return folder;
        }
        public static string GetDocumentIndexFolder(IWebHostEnvironment webHostEnvironment)
        {
            var folder = Path.Combine(webHostEnvironment.ContentRootPath, "dbs", "index");
            if(!Directory.Exists(folder)) { Directory.CreateDirectory(folder); }
            return folder;
        }
    }
}
