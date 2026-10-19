using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace RhinoCityJSON.SupportClasses
{
    public class ProjLoader
    {
        private const uint LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100;
        private const uint LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400;
        private const uint LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800;

        private static readonly object Sync = new object();
        private static IntPtr _nativeDdirectoryCookie = IntPtr.Zero;
        private static IntPtr _projModule = IntPtr.Zero;
        private static bool _initialized = false;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode,
            SetLastError = true, EntryPoint = "AddDllDirectory")]
        private static extern IntPtr AddDllDirectory(string newDirectory);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode,
            SetLastError = true, EntryPoint = "LoadLibraryExW")]
        private static extern IntPtr LoadLibraryEx(
            string fileName,
            IntPtr file,
            uint flags);

        public static void EnsureLoaded()
        {
            lock (Sync)
            {
                if (_initialized) { return; }
                    
                string pluginDirectory = Path.GetDirectoryName(typeof(ProjLoader).Assembly.Location);

                string nativeDirectory = Path.Combine( pluginDirectory, "native");
                string dataDirectory = Path.Combine( pluginDirectory, "proj-data");
                string projDll = Path.Combine( nativeDirectory, "proj_9.dll");

                if (!Directory.Exists(nativeDirectory))
                {
                    throw new DirectoryNotFoundException( "PROJ native directory not found: " + nativeDirectory);
                }

                if (!File.Exists(projDll))
                {
                    throw new FileNotFoundException( "PROJ DLL not found.", projDll);
                }

                if (!File.Exists(Path.Combine(dataDirectory, "proj.db")))
                {
                    throw new FileNotFoundException( "PROJ database not found.", Path.Combine(dataDirectory, "proj.db"));
                }

                // Configure PROJ before creating a PROJ context.
                Environment.SetEnvironmentVariable( "PROJ_DATA", dataDirectory);

                // Register the folder containing PROJ's dependencies.
                _nativeDdirectoryCookie = AddDllDirectory(nativeDirectory);

                if (_nativeDdirectoryCookie == IntPtr.Zero)
                {
                    throw new InvalidOperationException( "AddDllDirectory failed. Windows error: " +  Marshal.GetLastWin32Error());
                }

                // Load PROJ by its full path, with safe dependency search.
                uint flags =
                    LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR |
                    LOAD_LIBRARY_SEARCH_USER_DIRS |
                    LOAD_LIBRARY_SEARCH_SYSTEM32;

                _projModule = LoadLibraryEx(
                    projDll, IntPtr.Zero, flags);

                if (_projModule == IntPtr.Zero)
                {
                    throw new InvalidOperationException( "Could not load PROJ from: " + projDll +  ". Windows error: " +  Marshal.GetLastWin32Error());
                }

                // Do not unload PROJ or remove its directory registration
                // while this plugin is using it.
                _initialized = true;
            }
        }
    }

    public class ProjImplementation
    {
        private IntPtr context_;

        [DllImport("proj_9.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr proj_context_create();

        [DllImport("proj_9.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr proj_create( IntPtr context, [MarshalAs(UnmanagedType.LPStr)] string definition );

        [DllImport("proj_9.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int proj_is_crs(IntPtr obj);

        [DllImport("proj_9.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void proj_destroy(IntPtr obj);

        [DllImport("proj_9.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void proj_context_set_search_paths( IntPtr context,  int count, [In] string[] paths);

        public ProjImplementation()
        {
            ProjLoader.EnsureLoaded();
            context_ = proj_context_create();

            if (context_ == IntPtr.Zero)
                throw new InvalidOperationException(
                    "Could not initialize PROJ.");

            string dataPath = Path.Combine(  Path.GetDirectoryName( 
                typeof(ProjImplementation).Assembly.Location
                ),  "proj-data");

            proj_context_set_search_paths( context_, 1, new[] { dataPath });
        }

        public bool crsExists(string crs)
        {
            if (string.IsNullOrWhiteSpace(crs))  { return false; }

            IntPtr obj = proj_create(context_, crs);
            if(obj == IntPtr.Zero) { return false;  }

            int isCrs = proj_is_crs(obj);
            proj_destroy(obj);

            return isCrs != 0;
        }
    }
}
