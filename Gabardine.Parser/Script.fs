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
open Gabardine.Parser.Print
open Gabardine.Parser.Utils
open FParsec
open Compilers

type SyntaxBehaviour =
    | Func of int
    | RightAssociative of int
    | LeftAssociative of int
    | Prefix of int

type public Context() =
    let rw = new RewriteSystem()
    let lib = new ForeignLibrary()
    let root = new Namespace()
    do root.addSpecials()
    let files = new System.Collections.Generic.HashSet<string>()

    member x.Rewriter with get() = rw
    member x.Library with get() = lib
    member x.RootNamespace with get() = root

    member internal x.AddFile path = files.Add(System.IO.Path.GetFullPath(path)) |> ignore
    member x.IsParsed path = files.Contains(System.IO.Path.GetFullPath(path))

type public ScriptParser(ctx:Context) =

    let rw = ctx.Rewriter
    let library = ctx.Library

    let mutable dirStack = [System.Environment.CurrentDirectory];
    let pushDir d = 
        do dirStack <- d::dirStack
    let currentDir () = List.head dirStack
    let popDir () =
        do dirStack <- List.tail dirStack

    let infix = Infix()
    let expr = infix.Expr <??> "an infix expression"
    let tyParser = (TypeParser<ParseState>().typeParser .>> skip) <??> "a type"

    // Identifiers
    let identifier = 
        let valid = noneOf " \t\n(),:"
        many1Chars valid .>> skip

    let idpos = identifier .>>. getPosition

    // Operator declaration   
    let arityOrSyntax = 
        let func = pint32 |>> Func
        let rightAss = str_ws "right" >>. pint32 |>> RightAssociative
        let leftAss = str_ws "left" >>. pint32 |>> LeftAssociative
        let prefix = str_ws "prefix" >>. pint32 |>> Prefix
        choice [func; rightAss; leftAss; prefix]
    
    let aliases =
            idpos |>> fun n -> [n]
        <|> bracketed (many1 idpos)

    let op_dec state =        
        let decode names syntax pos =
            
            // Create the operator
            let (hname:string, _) = List.head names
            let alt = List.tail names |> Seq.map (fun (a,_) -> a) |> Seq.toArray
            let (opSyntax, arity) =
                match syntax with
                    | Func(a) -> (new OperatorSyntax(hname, OperatorSyntax.Fix.FunctionCall, 0, alt), a)
                    | Prefix(p) -> (new OperatorSyntax(hname, OperatorSyntax.Fix.Prefix, p, alt), 1)
                    | RightAssociative(p) -> (new OperatorSyntax(hname, OperatorSyntax.Fix.RightAssociative, p, alt), 2)
                    | LeftAssociative(p) -> (new OperatorSyntax(hname, OperatorSyntax.Fix.LeftAssociative, p, alt), 2)
            let op = new Operator(arity, OperatorKind.Function, opSyntax)

            // Tell the parser
            do infix.AddOperator(op)
            let ns = List.head state.scope
            let add (name,pos) =
                do ns.add(name, op, pos)
            List.iter add names
        str_ws "operator" >>. pipe3 aliases arityOrSyntax getPosition decode .>> skip

    let const_dec = 
        let decode state (name, pos) =
            let syntax = new OperatorSyntax(name)
            let op = new Operator(0, OperatorKind.Function, syntax)
            let ns = List.head state.scope
            ns.add(name, op, pos)
            // Declare as a constant
            rw.AddRule(Special.IsConst.CreateTerm(op.CreateTerm()), Special.True.CreateTerm())
        str_ws "constant" >>. pipe2 getUserState idpos decode
    
    // Rules
    let ruleLabel =
        (identifier .>>? str_ws ":" .>> skip) <??> "a rule label"

    let makeVariables state vars = 
        let check (name,pos) =
            match lookup state name 0 with
                | None -> ()
                | Some(op) -> 
                    do printYellow (pos.ToString())
                    do printCyan (sprintf "warning: variable %A will mask operator with same name defined at %s" name (op.pos.ToString()))
        let makeVar (name,pos) = 
            do check (name,pos) 
            do addVariable state name pos |> ignore
        List.iter makeVar vars

    let ruleScope =
        let forall = str_ws "forall" <|> str_ws "∀"
        forall >>. pipe2 getUserState (manyTill idpos (pchar ',')) makeVariables .>> skip

    let decodeRule conds label (raw:Term, pri) = 
        match Special.GetKind(raw.Op) with
            | Special.Kind.MapsTo -> rw.AddRule(RewriteRuleFactory.Create(raw.[0], raw.[1], conds, pri, true, label)) |> preturn
            | Special.Kind.Equals -> rw.AddEquivalence(raw.[0], raw.[1]) |> preturn
            | _ -> failFatally "Expected either a rewrite rule (->) or an equivalence (=)"

    let priority = 
            let something = str_ws "{" >>. str_ws "priority" >>. pint32 .>> skip .>> str_ws "}"
            something <|>% 0

    let rule = 
        let extractConditions (raw:Term) =
            let conds = ConsUtils.Disaggregate(Special.Implies, raw) |> Seq.cache
            let length = Seq.length conds
            (Seq.take (length-1) conds |> Seq.toList, Seq.last conds)

        let decode ((label, raw:Term), pri) =
            let (cnds, r) = extractConditions raw
            let cps = cnds |> Seq.map (fun c -> new ConditionPattern(c))
            decodeRule cps label (r, pri)

        
        
        let setup = (ruleLabel <|>% "") .>> optional ruleScope
        let body =  (setup .>>. expr .>>. priority) >>= decode
        push >>. body .>> pop 
     
    // Macro definition
    let macro = 
        let decodeLhs state pos name args =
            let arity =
                match args with
                    | Some(lst) -> 
                        do makeVariables state lst
                        List.length lst
                    | None -> 0
            addOperator arity OperatorKind.Function state name pos

        let defLhs = 
            let argList = bracketed (sepBy idpos (pchar ',' .>> skip)) .>> skip
            pipe4 getUserState getPosition identifier (opt argList) decodeLhs
        str_ws "def" >>. (lookAhead defLhs) >>. (expr .>>. priority >>= (decodeRule [] ""))

    // Foreign function definition
    let foreign =
        let arg = tyParser .>> skip .>> optional identifier
        let args = bracketed (sepBy arg (pchar ',' .>> skip))
        let decode retTy name argTypes =
            library.AddFunction(new ForeignFunction(name, argTypes, retTy))
        str_ws "foreign" >>. pipe3 tyParser identifier args decode

    // UI things
    let paramDef = 
        let decode prms state pos = 
            Seq.iter (fun p -> addParameter state p pos |> ignore) prms
        str_ws "param" >>. pipe3 (sepBy identifier (str_ws ",")) getUserState getPosition decode

    let rewrite =         
        let decode (vrb: option<_>) t = 
            if vrb.IsSome then Fancy.rewriteVerbose rw t else rw.RewriteUnordered(t)
        str_ws "rewrite" >>. pipe2 (opt (str_ws "verbose")) expr decode

    let superExpr = rewrite <|> expr 

    let print = 
        let printOption = 
                (str_ws "infix" >>% PrintFormat.Infix)
            <|> (str_ws "lisp" >>% PrintFormat.Lisp)
            <|> (str_ws "tree" >>% PrintFormat.Tree)
        str_ws "print" >>. pipe2 (opt printOption) superExpr Fancy.prettyprint

    let require, requireRef = createParserForwardedToRef ()

    let shell = 
        let exec args = 
            let (code, stdout, stderr) = execute "cmd.exe" ("/c " + args) ""
            Terminal.Stderr.Send(stderr)
            Terminal.Stdout.Send(stdout)
        str_ws "shell" >>. (restOfLine true) |>> exec

    let statement = 
        let ch = 
                (getUserState >>= op_dec)
            <|> const_dec
            <|> require
            <|> foreign
            <|> paramDef
            <|> print <|> (rewrite >>% ())
            <|> macro
            <|> shell
            <|> (rule <??> "a rule")
        ch .>> skip

    // Code emission
    let funcParser = FunctionParser(expr, statement).Parser

    let funcDef emitter = 
        str_ws "function" >>. push >>. skip >>. funcParser emitter rw .>> pop

    let moduleDef = 
        let decode name funcs = ()
            
        let curlies = between (str_ws "{") (str_ws "}")
        let body name =
            let emitter = LLVMFactory.Create library
            let compile funcs =
                let program = emitter.ToString();
                System.IO.File.WriteAllText("dump.il", program)
                Terminal.WriteLine(program)
                Compilers.compile name program
                Compilers.linkdll funcs name

            curlies (many (funcDef emitter)) |>> compile

        str_ws "module" >>. identifier >>= body 

    // Top-level
    let top = moduleDef <|> statement

    let full = skip >>. manyTill top eof

    let handleResult r =
        match r with
            | Success(_, _, _)   -> true
            | Failure(errorMsg, _, _) -> 
                printColor TerminalFormat.LightRed "Syntax error:\n" 
                Terminal.WriteLine errorMsg
                false

    let parseFile inpath =
        let path = System.IO.Path.Combine(currentDir(), inpath) |> System.IO.Path.GetFullPath        
        if ctx.IsParsed(path) then true else
            do System.IO.Path.GetDirectoryName(path) |> pushDir
            do sprintf "entering %s\n" path |> printGreen
            let init = {scope=[ctx.RootNamespace]}
            let result = runParserOnFile full init path System.Text.Encoding.UTF8 |> handleResult
            do popDir()
            if result then 
                ctx.AddFile(path)
                sprintf "leaving %s\n" path |> printGreen
            result

    let requireImpl =
        let decode path = 
            match parseFile path with
                | true -> preturn ()
                | false -> failFatally (sprintf "Error parsing required file %s" path)
        str_ws "require" >>. restOfLine false >>= decode
    do requireRef.Value <- requireImpl

    member x.Parse(script) =
        let init = {scope=[ctx.RootNamespace]}
        runParserOnString full init "" script |> handleResult

    member x.ParseFile(path) = parseFile path

    member x.ParseFiles(paths) =
        let count = Seq.map parseFile paths |> Seq.takeWhile id |> Seq.length
        count = Seq.length paths

       
        