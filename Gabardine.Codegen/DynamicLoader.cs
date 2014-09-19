/*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*/
using System;
using System.Runtime.InteropServices;

namespace Gabardine.Codegen
{
    public class DynamicLoader : IDisposable
    {
        readonly IntPtr handle;

        static class NativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, BestFitMapping =false, ThrowOnUnmappableChar =true, SetLastError = true)]
            public static extern IntPtr LoadLibrary(string libname);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, BestFitMapping =false, ThrowOnUnmappableChar =true, SetLastError = true)]
            public static extern bool FreeLibrary(IntPtr hModule);

            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, BestFitMapping =false, ThrowOnUnmappableChar =true, SetLastError = true)]
            public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
        }

        public DynamicLoader(string path)
        {
            handle = NativeMethods.LoadLibrary(path);
            if (handle == IntPtr.Zero) {
                int errorCode = Marshal.GetLastWin32Error();
                var inner = new System.ComponentModel.Win32Exception(errorCode);
                 throw new Exception("Error when trying to load " + path + "\n" + inner.Message, inner);
            }
        }

        public T GetFunction<T>(string functionName) where T : class
        {
            IntPtr funcaddr = NativeMethods.GetProcAddress(handle, functionName);
            if (funcaddr == IntPtr.Zero) {
                throw new MissingMethodException("Couldn't load method " + functionName);
            }
            return Marshal.GetDelegateForFunctionPointer(funcaddr, typeof(T)) as T;
        }

        ~DynamicLoader()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (handle != IntPtr.Zero) {
                NativeMethods.FreeLibrary(handle);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

}
