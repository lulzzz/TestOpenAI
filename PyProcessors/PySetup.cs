using Python.Included;
using Python.Runtime;
using System.Collections;
using System.Globalization;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;

namespace PyProcessors
{
    public static class PySetup
    {
        /// <summary>
        /// Initialized the Python environment and loads modules as necessary
        /// </summary>
        /// <param name="logAction">Logger to log any messages</param>
        /// <param name="pyLibModules">Extra set of optional Python libraries to install through <c>pip</c> e.g. <c>numpy</c></param>
        public static async Task<nint> Initialize(Action<string>? logAction = null, params string[] pyLibModules)
        {
            var librariesToLoad = PyLibraries.libraries
                .Split(',',StringSplitOptions.RemoveEmptyEntries)
                .Select(x=>x.Trim())
                .Union(pyLibModules, StringComparer.OrdinalIgnoreCase);
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Installer.InstallPath = assemblyPath; //install in local path

            if(logAction != null)
            {
                Installer.LogMessage += logAction;
            }
            // install the embedded python distribution
            await Installer.SetupPython();

            if (librariesToLoad.Any())
            {
                // install pip3 for package installation
                await Installer.TryInstallPip();
                foreach (var item in librariesToLoad)
                {
                    await Installer.PipInstallModule(item);
                }
            }
            PythonEngine.Initialize();
            return PythonEngine.BeginAllowThreads();
            
        }
        public static void Shutdown(nint? pythonState)
        {
            if (pythonState.HasValue)
            {
                PythonEngine.EndAllowThreads(pythonState.Value);
            }
            PythonEngine.Shutdown();
        }

    }
}