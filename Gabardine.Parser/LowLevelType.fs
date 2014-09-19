(*
Gabardine
Copyright (c) Microsoft Corporation
All rights reserved. 
Licensed under the Apache License, Version 2.0 (the License); you may not use this file except in compliance with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0 
THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR NON-INFRINGEMENT. 
See the Apache Version 2.0 License for specific language governing permissions and limitations under the License.
*)
namespace Gabardine.Parser

open Gabardine
open Gabardine.Codegen
open Gabardine.Parser.Utils
open FParsec

type TypeParser<'a>() =

    let voidParser = pstring "void" >>% LowLevelType.Void
    let nativeInt = pstring "native" >>% LowLevelType.NativeInteger
    let float64 = (pstring "float64" <|> pstring "double") >>% LowLevelType.Float64
    let integer = pstring "int" >>. pint32 |>> fun n -> LowLevelType.Integer(n, true)
    let unsignedInteger = pstring "uint" >>. pint32 |>> fun n -> LowLevelType.Integer(n, false)
    let baseType = choice [voidParser; nativeInt; float64; integer; unsignedInteger]

    let tp, tpRef = createParserForwardedToRef ()
    
    let strct = 
        let decode lst = (new Struct(lst)) :> LowLevelType
        let parser = pchar '<' >>. skip >>. sepBy1 tp (pchar ',' .>> skip) .>> pchar '>'
        parser |>> decode

    let full = 
        let decode t lst = List.fold (fun (x:LowLevelType) _ -> x.Pointer()) t lst
        pipe2 (strct <|> baseType) (many (pchar '*')) decode

    do tpRef.Value <- full
    
    member x.typeParser : Parser<LowLevelType,'a> = tp

type public TypeParser = 
    static member ParseType s =
        let parser = TypeParser<_>().typeParser
        match run parser s with
            | Success(result,_,_) -> result
            | Failure(msg, _, _) ->
                failwith (sprintf "Error parsing type: %s" msg)