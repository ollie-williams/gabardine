(*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*)
module Gabardine.Parser.Print

open Gabardine
open FParsec

let printColor col str =
    Terminal.Stdout.PushFormat(col)
    do sprintf "%s" str |> Terminal.Write
    Terminal.Stdout.PopFormat()

let printYellow<'a> = printColor TerminalFormat.Yellow
let printCyan<'a> = printColor TerminalFormat.LightCyan
let printGreen<'a> = printColor TerminalFormat.LightGreen

let posPrint s =
    let printer pos = 
        do pos.ToString() |> printColor TerminalFormat.DarkGray
        do sprintf "\n%O" s |> Terminal.WriteLine
    getPosition |>> printer


