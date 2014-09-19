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
open Gabardine.Parser.Print
open Gabardine.Parser.Utils
open FParsec

type public Infix() =
    
    let validId =
        let valid = noneOf " \t\n^*/+-><&|,()[];\"=";
        many1Chars valid
         
    let identifier =
        validId .>> skip

    let typeParser = TypeParser<ParseState>().typeParser
    let type_ = 
        let decode ty = Term.Const(ty)
        between (str_ws "<") (str_ws ">") typeParser |>> decode

    let number  =
        let numberFormat = NumberLiteralOptions.AllowMinusSign
                       ||| NumberLiteralOptions.AllowFraction
                       ||| NumberLiteralOptions.AllowExponent
                       ||| NumberLiteralOptions.AllowHexadecimal
                       ||| NumberLiteralOptions.AllowSuffix

        let parser = numberLiteral numberFormat "number"
        let decode (nl:NumberLiteral) =
            if nl.IsInteger then
                match (nl.SuffixChar1, nl.SuffixChar2) with
                    | ('u', 'l') -> Term.Const(uint64 nl.String)
                    | ('u', _) -> Term.Const(uint32 nl.String)
                    | ('l', _) -> Term.Const(int64 nl.String)
                    | _ -> Term.Const(int32 nl.String)
            else
                let flt = if nl.IsHexadecimal then floatOfHexString nl.String else float nl.String
                Term.Const(flt)
        parser |>> decode .>> skip

    let stringLiteral =
        let decode str = Term.Const(str)
        pchar '"' >>. manyCharsTill anyChar (pchar '"') |>> decode
   
    let opp = new OperatorPrecedenceParser<Term, ParseState, ParseState>()
    let expr = opp.ExpressionParser 

    let makeBoundVariable (v:Operator) state pos name =
        let aux () =
            let ns = (List.head state.scope)
            do ns.add(v, pos)
            preturn v

        match lookup state name 0 with 
        | None -> aux ()
        | Some(op) -> 
            match (op.op.Kind) with
            | OperatorKind.LambdaVariable -> failFatally (sprintf "There is already a bound variable %s in scope from %s" name (op.pos.ToString()))
            | OperatorKind.LetVariable -> failFatally (sprintf "There is already a bound variable %s in scope from %s" name (op.pos.ToString()))
            | OperatorKind.PatternVariable -> preturn op.op
            | _ -> 
                do printCyan (sprintf "warning: variable %A will mask operator with same name defined at %s" name (op.pos.ToString())) 
                aux ()

    let lambda  = 
        let decode (raw:Term) =
            do System.Diagnostics.Debug.Assert(raw.Op.Equals(Special.MapsTo))
            let (vble, body) = (raw.[0], raw.[1])
            Special.Lambda.CreateTerm(vble, body)

        let makeVar (state, pos, name) = makeBoundVariable (new LambdaVariable(name)) state pos name
        let createVar = 
            tuple3 getUserState getPosition (lookAhead identifier) >>= makeVar

        str_ws "λ" >>. push >>. (createVar >>. expr |>> decode) .>> pop

    let letExpr =
        let decode (var, bnd) body =
            Special.Let.CreateTerm(var, bnd, body)

        let makeVar (state, pos, name, bnd) = 
            makeBoundVariable (new LetVariable(name, bnd)) state pos name 
            |>> fun v -> (v.CreateTerm(), bnd)
             
        let binding = 
            tuple4 getUserState getPosition (identifier .>> (str_ws "=")) expr >>= makeVar

        let body = optional (str_ws "in") >>. expr

        str_ws "let" >>. push >>. pipe2 binding body decode .>> pop .>> skip


    let wildcard = 
        let wcterm = Special.Wildcard.CreateTerm()
        pchar '_' >>% wcterm .>> skip

    let nullary state =  
        let lookup name =            
            match lookup state name 0 with
                | Some(op) -> preturn (op.op.CreateTerm())
                | _ -> failFatally (sprintf "Nullary operator %s not found" name)
        identifier >>= lookup

    let fncall state  =  
        let argList = sepBy expr (pchar ',' .>> skip) .>> skip
        let parse = (*identifier*) validId .>>.? between (str_ws "(") (str_ws ")") argList
        let decode (name,args) =
            let arity = List.length args
            match lookup state name arity with
                | Some(op) -> preturn (op.op.CreateTerm(args))
                | None -> failFatally (sprintf "Operator %s with arity %d not found" name arity)
        parse >>= decode

    let codegenSugar = 
        let translate (lhs, rhs) = Special.MapsTo.CreateTerm(lhs, rhs)
        let sugar = CodegenParser(expr).Parser
        str_ws "codegen" >>. sugar |>> translate


    let primative state =
        number
        <|> type_
        <|> wildcard
        <|> stringLiteral
        <|> lambda
        <|> letExpr
        <|> codegenSugar
        <|> (fncall state)
        <|> (nullary state)
        <|> between (str_ws "(") (str_ws ")") expr


    let index state = 
        let subscript = (str_ws "[") >>? expr .>> (str_ws "]") <??> "a subscript"
        let decode (bse, off) = Special.Index.CreateTerm(bse, off)
        ((primative state) .>>.? subscript) |>> decode

    do opp.TermParser <- 
            (getUserState >>= index <??> "an array")
        <|> (getUserState >>= primative <??> "a primative")

    let infixOp name prec ass = 
        let impl state l r = 
            match lookup state name 2 with
                | Some(op) -> op.op.CreateTerm(l, r)
                | None -> sprintf "Couldn't find binary operator %s" name |> failwith
        InfixOperator(name, getUserState .>> skip, prec, ass, (), impl)

    let prefixOp name prec =
        let impl state (arg:Term) =
            match lookup state name 1 with
                | Some(op) -> op.op.CreateTerm(arg)
                | None -> sprintf "Couldn't find unary operator %s" name |> failwith
        PrefixOperator(name, getUserState .>> skip, prec, true, (), impl)


    let addMany fn (syntax:OperatorSyntax) =
        fn syntax.Precedence syntax.Name
        syntax.AlternateNames |> Array.iter (fn syntax.Precedence)

    let addOp (op:Operator) =
        match op.Syntax.Style with
        | OperatorSyntax.Fix.LeftAssociative -> addMany (fun p n -> opp.AddOperator (infixOp n p Associativity.Left)) op.Syntax
        | OperatorSyntax.Fix.RightAssociative -> addMany (fun p n -> opp.AddOperator (infixOp n p Associativity.Right)) op.Syntax
        | OperatorSyntax.Fix.Prefix -> addMany (fun p n -> opp.AddOperator (prefixOp n p)) op.Syntax
        | OperatorSyntax.Fix.Postfix -> failwith "Not implemented"
        | _ -> ()

    do Special.All |> Array.iter addOp

    let full = skip >>. expr .>> eof

    member x.AddOperator op = addOp op

    member x.Expr = expr

    member x.Parse ns s = 
        let init = {scope=[ns]}
        match runParserOnString full init "" s with
            | Success(result, _, _)   -> result
            | Failure(errorMsg, _, _) -> 
                failwith (sprintf "Syntax error: %s" errorMsg)
