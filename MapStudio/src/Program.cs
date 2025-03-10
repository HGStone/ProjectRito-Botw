﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using OpenTK.Graphics;
using Toolbox.Core;
using GLFrameworkEngine;
using System.Reflection;
using MapStudio.UI;
using HackOpenGL;

namespace MapStudio
{
    public class Program
    {
        static bool IS_DEBUG = false;

        const string DLL_DIRECTORY = "Lib";

        static void Main(string[] args)
        {
            //Set global for method that compiles during debug building.
            IsDebugCheck(ref IS_DEBUG);

            //Hide the console unless debugging
            #if Windows
            if (!IS_DEBUG)
                ConsoleWindowUtil.Hide();
            else
                ConsoleWindowUtil.Show();
            #endif

             // 默认：选择 NVIDIA 显卡
            OpenGLVendorSelector.ChooseOpenGLVendor(OpenGLVendorSelector.VENDOR_NVIDIA);

            //Assembly searching from folders
            var domain = AppDomain.CurrentDomain;
            domain.AssemblyResolve += LoadAssembly;
            //Arguments in the command line
            var argumentHandle = LoadCmdArguments(args);
            if (argumentHandle.SkipWindow)
                return;

            //Global variables across the application
            InitRuntime();
            //Reload the language keys
            TranslationSource.Instance.Reload();
            //Initiate the texture resource creator for making texture instances from STGenericTexture.
            InitGLResourceCreation();
            //Load the window and run the application
            GraphicsMode mode = new GraphicsMode(new ColorFormat(32), 24, 8, 4, new ColorFormat(32), 2, false);
            MainWindow wnd = new MainWindow(mode, argumentHandle);
            wnd.OriginalIcon = wnd.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            wnd.VSync = OpenTK.VSyncMode.On;
            wnd.Run();
        }

        [Conditional("DEBUG")]
        static void IsDebugCheck(ref bool isDebug) {
            isDebug = true;
        }

        static void InitRuntime()
        {
            //Global variables across the application
            Runtime.ExecutableDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Runtime.DisplayBones = false;
            Runtime.BonePointSize = 0.7f;
            Runtime.OpenTKInitialized = true;

            Directory.SetCurrentDirectory(Runtime.ExecutableDir);
        }

        static Arguments LoadCmdArguments(string[] args)
        {
            Arguments argumentHandle = new Arguments();
            foreach (var arg in args)
            {
                //Autmatically load files that are input into the command line.
                if (File.Exists(arg))
                    argumentHandle.FileInput.Add(arg);
            }
            return argumentHandle;
        }

        //Render creation for the opengl backend
        //This is to keep the render handling more seperated from the core library
        static void InitGLResourceCreation()
        {
            //Called during LoadRenderable() in STGenericTexture to set the RenderableTex instance.
            RenderResourceCreator.CreateTextureInstance += TextureCreationOpenGL;
        }

        static IRenderableTexture TextureCreationOpenGL(object sender, EventArgs e)
        {
            var tex = sender as STGenericTexture;
            return GLTexture.FromGenericTexture(tex, tex.Parameters);
        }

        /// 
        /// Include externals dlls
        /// 
        private static Assembly LoadAssembly(object sender, ResolveEventArgs args)
        {
            Assembly result = null;
            if (args != null && !string.IsNullOrEmpty(args.Name))
            {
                //Get current exe fullpath
                FileInfo info = new FileInfo(Assembly.GetExecutingAssembly().Location);

                //Get folder of the executing .exe
                var folderPath = Path.Combine(info.Directory.FullName, DLL_DIRECTORY);  

                //Build potential fullpath to the loading assembly
                var assemblyName = args.Name.Split(new string[] { "," }, StringSplitOptions.None)[0];
                var assemblyExtension = "dll";
                var assemblyPath = Path.Combine(folderPath, string.Format("{0}.{1}", assemblyName, assemblyExtension));

                //Check if the assembly exists in our "Libs" directory
                if (File.Exists(assemblyPath))
                {
                    //Load the required assembly using our custom path
                    result = Assembly.LoadFrom(assemblyPath);
                }
                else
                {
                    //Keep default loading
                    return args.RequestingAssembly;
                }
            }

            return result;
        }

        public class Arguments
        {
            public List<string> FileInput = new List<string>();

            public bool SkipWindow = false;
        }
    }
}
