using System.ComponentModel;

namespace TestOpenAI.Helpers
{
    public class AppData
    {
        private string _appName = string.Empty;
        public string AppName { 
            get => _appName;
            set
            {
                _appName = value;
                AppNameChanged?.Invoke(this, _appName);
            }
        }
        public EventHandler<string>? AppNameChanged { get; set; }
        
    }
}
