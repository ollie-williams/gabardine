(*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*)
namespace Gabardine.Parser

open System.Collections.Generic
open FParsec
open Gabardine
open Gabardine.Parser.Print

type NameArity = 
    {name:string; arity:int}
    override x.ToString() = sprintf "%s(%d)" x.name x.arity

type MarkedOperator = {op:Operator; pos:Position}

type public Namespace() =
    
    let dict = new Dictionary<NameArity, MarkedOperator>()
        
    member internal x.lookup name arity = 
        let (success, value) = dict.TryGetValue({name=name; arity=arity})
        if success then Some(value) else None

    member internal x.add (name, op:Operator, pos) =
        let key = {name=name; arity=op.Arity}
        let result, existing = dict.TryGetValue(key)
        if (result) then 
            let msg = sprintf "Operator %s, arity %d, was already declared at %A." name op.Arity existing.pos 
            printCyan msg
            //failwith msg
        do dict.Add(key, {op=op; pos=pos})

    member internal x.add (op:Operator, pos) = 
        x.add (op.Name, op, pos)
        op.Syntax.AlternateNames |> Array.iter (fun n -> x.add (n, op, pos))

    member x.add (op:Operator) = x.add (op, new Position("special", 0L, 0L, 0L))

    member x.addSpecials () = 
        do Special.All |> Seq.iter x.add
        x.add Gabardine.Codegen.Instructions.Generate
        x.add Gabardine.Codegen.OutputDirectives.OutParam
        x.add Gabardine.Codegen.OutputDirectives.ReturnValue

    member x.TryGetOperator(name, arity, [<System.Runtime.InteropServices.Out>] result : Operator byref) =
        match x.lookup name arity with
            | Some(op) -> 
                do result <- op.op
                true
            | None -> false



