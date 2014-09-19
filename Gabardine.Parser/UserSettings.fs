(*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*)
module Gabardine.UserSettings

open FSharp.Data

[<Literal>]
let settingsExample = """{
	"llc" : {
		"exepath" : "C:/dev/llvm/bin/llc.exe"
	},

	"link" : {
		"x86" : {
			"exepath" : "C:/Program Files (x86)/Microsoft Visual Studio 12.0/VC/BIN/link.exe",
			"dirs" : ["C:/Program Files (x86)/Microsoft Visual Studio 12.0/VC/lib",
					  "C:/Program Files (x86)/Windows Kits/8.1/Lib/winv6.3/um/x86",
					  "C:/mkl/lib/ia32"],
			"libs" : ["mkl_intel_c.lib", "mkl_sequential.lib", "mkl_core.lib", "libiomp5md.lib"]
		},

		"x64" : {
			"exepath" : "C:/Program Files (x86)/Microsoft Visual Studio 12.0/VC/BIN/amd64_x86/link.exe",
			"dirs" : ["C:/Program Files (x86)/Microsoft Visual Studio 12.0/VC/lib/amd64",
					  "C:/Program Files (x86)/Windows Kits/8.1/Lib/winv6.3/um/x64",
					  "C:/mkl/lib/intel64"],
			"libs" : ["mkl_intel_ilp64.lib", "mkl_sequential.lib", "mkl_core.lib"]
		}
	}
}"""

type Settings = JsonProvider<settingsExample>

let mutable settings = null

let LoadSettings (file:string) = 
    do settings <- Settings.Load(file)
