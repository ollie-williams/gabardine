(*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*)
module Compilers

open System.IO

let execute exePath args (inStream:string) =
    use p = new System.Diagnostics.Process()
    p.StartInfo.FileName <- exePath
    p.StartInfo.Arguments <- args
    p.StartInfo.UseShellExecute <- false
    p.StartInfo.RedirectStandardInput <- true
    p.StartInfo.RedirectStandardOutput <- true
    p.StartInfo.RedirectStandardError <- true
    System.Diagnostics.Trace.WriteLine(sprintf "%s %s" exePath args)
    p.Start() |> ignore

    if inStream.Length > 0 then 
        p.StandardInput.Write(inStream)
        p.StandardInput.Close()
    let out = p.StandardOutput.ReadToEnd()
    let err = p.StandardError.ReadToEnd()
    do p.WaitForExit()    
    (p.ExitCode, out, err)

type Architecture = 
    | X86 
    | X64 

let arch =
    match System.IntPtr.Size with
        | 4 -> X86
        | 8 -> X64
        | _ -> failwith "Unknown architecture"

let compile name assembly =
    let exepath = Gabardine.UserSettings.settings.Llc.Exepath
    let arguments = 
        let march =
            match arch with
                | X86 -> "x86"
                | X64 -> "x86-64"
        sprintf "-O3 -march=%s -filetype=obj -o=%s.obj" march name
    let (exit, stdout, stderr) = execute exepath arguments assembly 
    if exit <> 0 then 
        sprintf "exit=%d\n%s" exit stderr |> Gabardine.Terminal.Stderr.SendLine
        failwith "LLC failed"
    

let linkdll functionNames name =
    let lnk = 
        match arch with
            | X86 -> Gabardine.UserSettings.settings.Link.X86
            | X64 -> Gabardine.UserSettings.settings.Link.X64
    let dirs = 
        lnk.Dirs
            |> Seq.map (fun n -> sprintf "/LIBPATH:\"%s\" " n) 
            |> Seq.reduce (fun a b -> a + b)
    let libs = 
        lnk.Libs
            |> Seq.map (fun n -> sprintf "%s " n) 
            |> Seq.reduce (fun a b -> a + b)
    let exports = 
        functionNames
            |> Seq.map (fun n -> sprintf "/EXPORT:%s " n) 
            |> Seq.reduce (fun a b -> a + b)
    let arguments = sprintf "/NOLOGO /DLL %s %s msvcrt.lib %s %s.obj" dirs libs exports name
    let (exit, stdout, stderr) = execute lnk.Exepath arguments ""
    if exit <> 0 then 
        sprintf "exit=%d\n%s" exit stdout |> Gabardine.Terminal.Stderr.SendLine
        failwith "Linker failed"
   