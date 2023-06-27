using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PyProcessors.Models
{
    public class AIResponse
    {
        public string Answer { get; set; }
        public List<SourceDocument> SourceDocuments { get; set; } = new();
    }
}
