using System;
using System.Collections.Generic;
using System.IO;
using HackCompiler.Tokens;

namespace HackCompiler
{
    public class Compiler
    {
        private readonly List<Token> _tokens;
        private readonly StringWriter _writer;
        private readonly VMWriter _vm;
        private string _output;
        private int _tokenCounter;
        private string _currentClass;
        private string _currentSubroutine;
        private int _ifCount;
        private int _whileCount;

        public Compiler(List<Token> tokens)
        {
            _tokens = tokens;
            _writer = new StringWriter();
            _vm = new VMWriter(_writer);
        }

        private Token GetNext()
        {
            if (_tokenCounter >= _tokens.Count)
            {
                return null;
            }

            _tokenCounter++;

            return _tokens[_tokenCounter - 1];
        }

        private Token PeekNext()
        {
            if (_tokenCounter >= _tokens.Count)
            {
                return null;
            }

            return _tokens[_tokenCounter];
        }

        private void OutputToken(Token token)
        {
            switch (token.Type)
            {
                case TokenType.Identifier:
                    _output += "<identifier> " + token.Value + " </identifier>\n";
                    break;
                case TokenType.IntegerConstant:
                    _output += "<integerConstant> " + token.Value + " </integerConstant>\n";
                    break;
                case TokenType.Keyword:
                    _output += "<keyword> " + token.Value + " </keyword>\n";
                    break;
                case TokenType.StringConstant:
                    _output += "<stringConstant> " + token.Value.Replace("\"", string.Empty) + " </stringConstant>\n";
                    break;
                case TokenType.Symbol:
                    _output += "<symbol> " + token.Value + " </symbol>\n";
                    break;
            }
        }

        private bool ValidateType(Token token)
        {
            if (!(token.Value == "int" || token.Value == "boolean" || token.Value == "char" || token.Type == TokenType.Identifier))
            {
                return false;
            }

            return true;
        }

        private string GetSegmentFor(SymbolKind kind)
        {
            string segment = "";
            
            switch (kind)
            {
                case SymbolKind.Variable:
                    segment =  "local";
                    break;
                case SymbolKind.Argument:
                    segment = "argument";
                    break;
                case SymbolKind.Field:
                    segment = "this";
                    break;
                case SymbolKind.Static:
                    segment = "static";
                    break;
            }

            return segment;
        }

        public string Compile()
        {
            //reset
            _output = string.Empty;
            _tokenCounter = 0;

            CompileClass();

            _output = _writer.ToString();

            _writer.Dispose();

            return _output;
        }

        private void CompileClass()
        {
            var token = GetNext(); // class

            if (token.Value != "class")
            {
                throw new Exception("Missing keyword \"class\"");                
            }

            token = GetNext(); // identifier (class name)

            if (token.Type != TokenType.Identifier)
            {
                throw new Exception("Missing identifier");
            }

            //Save current class name
            _currentClass = token.Value;

            token = GetNext(); // {

            if (token.Value != "{")
            {
                throw new Exception("Missing '{'");
            }

            while (PeekNext().Value == "static" || PeekNext().Value == "field")
            {
                CompileClassVarDeclaration();
            }

            while (PeekNext().Value == "constructor" || PeekNext().Value == "function" || PeekNext().Value == "method")
            {
                CompileSubroutine();
            }

            token = GetNext(); // }

            if (token.Value != "}")
            {
                throw new Exception("Missing '}'");
            }
        }

        private void CompileClassVarDeclaration()
        {
            var token = GetNext(); //static | field

            if (token.Value != "static" && token.Value != "field")
            {
                throw new Exception("Missing static|field");
            }

            var kind = token.Value == "static" ? SymbolKind.Static : SymbolKind.Field;

            token = GetNext(); //type

            if (!ValidateType(token))
            {
                throw new Exception("Invalid type.  Must be int|char|boolean or class");
            }

            var type = token.Value;

            token = GetNext(); //identifier

            if (token.Type != TokenType.Identifier)
            {
                throw new Exception("Missing identifier");
            }

            var varName = token.Value;

            SymbolTable.Define(varName, type, kind);

            while (PeekNext().Value == ",")
            {
                token = GetNext(); // ,

                token = GetNext(); //identifier

                if (token.Type != TokenType.Identifier)
                {
                    throw new Exception("Missing identifier");
                }

                varName = token.Value;

                SymbolTable.Define(varName, type, kind);
            }

            token = GetNext(); // ;

            if (token.Value != ";")
            {
                throw new Exception("Missing ';'");
            }
        }

        private void CompileSubroutine()
        {
            //Reset symbol table
            SymbolTable.StartNewSubroutine();

            //Reset label counts
            _ifCount = 0;
            _whileCount = 0;

            var token = GetNext(); //subroutine type

            if (!(token.Value == "constructor" || token.Value == "function" || token.Value == "method"))
            {
                throw new Exception("Missing constructor|function|method");
            }

            var subroutineType = token.Value;

            token = GetNext(); //return type

            if (!(token.Value == "void" || ValidateType(token)))
            {
                throw new Exception("Missing void or valid type");
            }

            token = GetNext(); //subroutine name

            if (token.Type != TokenType.Identifier)
            {
                throw new Exception("Missing identifier");
            }

            _currentSubroutine = token.Value;

            token = GetNext(); // (

            if (token.Value != "(")
            {
                throw new Exception("Missing '('");
            }

            if (subroutineType == "method")
            {
                //Add implicit this parameter
                SymbolTable.Define("__this", "null", SymbolKind.Argument);
            }

            CompileParameterList();

            token = GetNext(); // )

            if (token.Value != ")")
            {
                throw new Exception("Missing ')'");
            }

            token = GetNext(); // {

            if (token.Value != "{")
            {
                throw new Exception("Missing '{'");
            }

            while (PeekNext().Value == "var")
            {
                CompileVarDec();
            }

            //Know the number of locals, so can write the function
            var argCount = SymbolTable.GetCount(SymbolKind.Variable);
            _vm.Function(_currentClass + "." + _currentSubroutine, argCount);

            //if this is a constructor, we need to allocate memory
            if (subroutineType == "constructor")
            {
                _vm.Push("constant", SymbolTable.GetCount(SymbolKind.Field));
                _vm.Call("Memory.alloc", 1);

                //set THIS pointer
                _vm.Pop("pointer", 0);
            }

            //if this is a method, need to handle invisible this argument
            if (subroutineType == "method")
            {
                _vm.Push("argument", 0);
                _vm.Pop("pointer", 0);
            }

            while (PeekNext().Value != "}")
            {
                CompileStatements();
            }

            token = GetNext(); // }

            if (token.Value != "}")
            {
                throw new Exception("Missing '}'");
            }
        }

        private void CompileParameterList()
        {
            //Empty parameter list
            if (PeekNext().Value == ")")
            {
                return;
            }

            var token = GetNext(); // param type

            if (!ValidateType(token))
            {
                throw new Exception("Missing type");
            }

            var paramType = token.Value;

            token = GetNext(); // param name

            if (token.Type != TokenType.Identifier)
            {
                throw new Exception("Missing identifier");
            }

            var paramName = token.Value;

            SymbolTable.Define(paramName, paramType, SymbolKind.Argument);

            //Additional parameters
            while (PeekNext().Value == ",")
            {
                token = GetNext(); // ,

                token = GetNext(); // param type

                if (!ValidateType(token))
                {
                    throw new Exception("Missing type");
                }

                paramType = token.Value;

                token = GetNext(); // param name

                if (token.Type != TokenType.Identifier)
                {
                    throw new Exception("Missing identifier");
                }

                paramName = token.Value;

                SymbolTable.Define(paramName, paramType, SymbolKind.Argument);
            }
        }

        private void CompileVarDec()
        {
            var token = GetNext(); // var

            token = GetNext(); //type

            var varType = token.Value;

            if (!ValidateType(token))
            {
                throw new Exception("Missing type");
            }

            token = GetNext(); // variable name

            var varName = token.Value;

            if (token.Type != TokenType.Identifier)
            {
                throw new Exception("Missing identifier");
            }

            SymbolTable.Define(varName, varType, SymbolKind.Variable);

            while (PeekNext().Value == ",")
            {
                token = GetNext(); // ,

                token = GetNext(); // variable name

                varName = token.Value;

                if (token.Type != TokenType.Identifier)
                {
                    throw new Exception("Missing identifier");
                }
                
                SymbolTable.Define(varName, varType, SymbolKind.Variable);
            }

            token = GetNext(); // ;

            if (token.Value != ";")
            {
                throw new Exception("Missing ';'");
            }
        }

        private void CompileStatements()
        {
            while (PeekNext().Value == "let" || PeekNext().Value == "if" || PeekNext().Value == "while" || PeekNext().Value == "do" || PeekNext().Value == "return")
            {
                switch (PeekNext().Value)
                {
                    case "let":
                        CompileLetStatement();
                        break;
                    case "if":
                        CompileIfStatement();
                        break;
                    case "while":
                        CompileWhileStatement();
                        break;
                    case "do":
                        CompileDoStatement();
                        break;
                    case "return":
                        CompileReturnStatement();
                        break;
                }
            }
        }

        private void CompileLetStatement()
        {
            bool isArray = false;
            var token = GetNext(); // let

            token = GetNext(); // lvalue

            if (token.Type != TokenType.Identifier)
            {
                throw new Exception("Missing identifier");
            }

            var lvalue = SymbolTable.GetSymbol(token.Value);

            if (lvalue == null)
            {
                throw new Exception("Unknown identifier: " + token.Value);
            }

            var segment = GetSegmentFor(lvalue.Kind);

            // Handle array
            if (PeekNext().Value == "[")
            {
                isArray = true;

                token = GetNext(); // [

                CompileExpression();

                token = GetNext(); // ]

                if (token.Value != "]")
                {
                    throw new Exception("Missing ']'");
                }

                _vm.Push(segment, lvalue.Index);
                _vm.Arithmetic("add");
            }

            token = GetNext(); // =

            if (token.Value != "=")
            {
                throw new Exception("Missing '='");
            }

            CompileExpression();

            token = GetNext(); //;

            if (token.Value != ";")
            {
                throw new Exception("Missing ';'");
            }

            if (isArray)
            {
                _vm.Pop("temp", 0);
                _vm.Pop("pointer", 1);
                _vm.Push("temp", 0);
                _vm.Pop("that", 0);
            }

            else
            {
                _vm.Pop(segment, lvalue.Index);
            }
        }

        private void CompileIfStatement()
        {
            var currentCount = _ifCount;

            //advance if count
            _ifCount++;

            var token = GetNext(); // if

            token = GetNext(); // (

            if (token.Value != "(")
            {
                throw new Exception("Missing '('");
            }

            CompileExpression();

            token = GetNext(); // )

            if (token.Value != ")")
            {
                throw new Exception("Missing ')'");
            }

            token = GetNext(); // {

            if (token.Value != "{")
            {
                throw new Exception("Missing '{'");
            }

            _vm.If("IF_TRUE" + currentCount);
            _vm.Goto("IF_FALSE" + currentCount);
            _vm.Label("IF_TRUE" + currentCount);

            CompileStatements();

            token = GetNext(); // }

            if (token.Value != "}")
            {
                throw new Exception("Missing '}'");
            }

            if (PeekNext().Value == "else")
            {
                _vm.Goto("IF_END" + currentCount);
                _vm.Label("IF_FALSE" + currentCount);

                token = GetNext(); // else

                token = GetNext(); // {

                if (token.Value != "{")
                {
                    throw new Exception("Missing '{'");
                }

                CompileStatements();

                token = GetNext(); // }

                if (token.Value != "}")
                {
                    throw new Exception("Missing '}'");
                }

                _vm.Label("IF_END" + currentCount);
            }

            else
            {
                _vm.Label("IF_FALSE" + currentCount);
            }
        }

        private void CompileWhileStatement()
        {
            var currentCount = _whileCount;

            //advance count
            _whileCount++;

            var token = GetNext(); // while

            token = GetNext(); // (

            if (token.Value != "(")
            {
                throw new Exception("Missing '('");
            }

            _vm.Label("WHILE_EXP" + currentCount);
            
            CompileExpression();

            //compute inverse of condition

            _vm.Arithmetic("not");

            token = GetNext(); // )

            if (token.Value != ")")
            {
                throw new Exception("Missing ')'");
            }

            token = GetNext();

            if (token.Value != "{")
            {
                throw new Exception("Missing '{'");
            }

            _vm.If("WHILE_END" + currentCount);

            CompileStatements();

            token = GetNext(); // }

            if (token.Value != "}")
            {
                throw new Exception("Missing '}'");
            }

            _vm.Goto("WHILE_EXP" + currentCount);
            _vm.Label("WHILE_END" + currentCount);
        }

        private void CompileDoStatement()
        {
            var token = GetNext(); // do

            token = GetNext(); //subroutine | (className | varName)

            //Calling method of current class
            if (PeekNext().Value == "(")
            {
                var subroutineName = token.Value;

                token = GetNext(); // (

                //push invisible THIS first param
                _vm.Push("pointer", 0);

                var paramCount = CompileExpressionList();

                token = GetNext(); // )

                if (token.Value != ")")
                {
                    throw new Exception("Missing ')'");
                }

                _vm.Call(_currentClass + "." + subroutineName, paramCount + 1);
            }

            //
            else if (PeekNext().Value == ".")
            {
                bool isMethod = true;
                var varName = token.Value;
                var variable = SymbolTable.GetSymbol(varName);

                if (variable == null)
                {
                    isMethod = false;
                }

                token = GetNext(); // .

                token = GetNext(); // subroutine name

                var subroutineName = token.Value;

                token = GetNext(); // (

                if (token.Value != "(")
                {
                    throw new Exception("Missing '('");
                }

                var paramCount = 0;

                if (isMethod)
                {
                    var segment = GetSegmentFor(variable.Kind);
                    _vm.Push(segment, variable.Index);
                    paramCount++;
                }

                paramCount += CompileExpressionList();

                token = GetNext(); // )

                if (token.Value != ")")
                {
                    throw new Exception("Missing ')'");
                }

                if (isMethod)
                {
                    _vm.Call(variable.Type + "." + subroutineName, paramCount);
                }

                else
                {
                    _vm.Call(varName + "." + subroutineName, paramCount);
                }
            }

            token = GetNext(); //;

            if (token.Value != ";")
            {
                throw new Exception("Missing ';'");
            }

            //handle void return value
            _vm.Pop("temp", 0);
        }

        private void CompileReturnStatement()
        {
            var token = GetNext(); // return

            if (PeekNext().Value != ";")
            {
                CompileExpression();
            }

            //void return
            else
            {
                _vm.Push("constant", 0);
            }

            token = GetNext(); // ;

            if (token.Value != ";")
            {
                throw new Exception("Missing ';'");
            }

            _vm.Return();
        }

        private void CompileExpression()
        {
            CompileTerm();

            while (PeekNext().Value == "+"
                || PeekNext().Value == "-"
                || PeekNext().Value == "*"
                || PeekNext().Value == "/"
                || PeekNext().Value == "&"
                || PeekNext().Value == "|"
                || PeekNext().Value == "<"
                || PeekNext().Value == ">"
                || PeekNext().Value == "=")
            {
                var token = GetNext(); // op

                var operation = token.Value;

                CompileTerm();

                switch (operation)
                {
                    case "+":
                        _vm.Arithmetic("add");
                        break;
                    case "-":
                        _vm.Arithmetic("sub");
                        break;
                    case "*":
                        _vm.Call("Math.multiply", 2);
                        break;
                    case "/":
                        _vm.Call("Math.divide", 2);
                        break;
                    case "&":
                        _vm.Arithmetic("and");
                        break;
                    case "|":
                        _vm.Arithmetic("or");
                        break;
                    case "<":
                        _vm.Arithmetic("lt");
                        break;
                    case ">":
                        _vm.Arithmetic("gt");
                        break;
                    case "=":
                        _vm.Arithmetic("eq");
                        break;
                }
            }
        }

        private void CompileTerm()
        {
            Token token;

            if (PeekNext().Type == TokenType.IntegerConstant)
            {
                token = GetNext();

                var value = Int32.Parse(token.Value);

                _vm.Push("constant", value);
            }

            else if (PeekNext().Type == TokenType.StringConstant)
            {
                token = GetNext(); //the string constant surrounded by quotes

                var value = token.Value.Replace("\"", string.Empty);

                _vm.Push("constant", value.Length);
                _vm.Call("String.new", 1);

                foreach (char c in value)
                {
                    _vm.Push("constant", Convert.ToInt32(c));
                    _vm.Call("String.appendChar", 2);
                }
            }

            else if (PeekNext().Type == TokenType.Keyword)
            {
                token = GetNext();

                if (!(token.Value == "true" || token.Value == "false" || token.Value == "null" || token.Value == "this"))
                {
                    throw new Exception("Invalid keyword");
                }

                switch (token.Value)
                {
                    case "true":
                        _vm.Push("constant", 0);
                        _vm.Arithmetic("not");
                        break;
                    case "false":
                        _vm.Push("constant", 0);
                        break;
                    case "this":
                        _vm.Push("pointer", 0);
                        break;
                    case "null":
                        _vm.Push("constant", 0);
                        break;
                }
            }

            else if (PeekNext().Type == TokenType.Identifier)
            {
                token = GetNext(); //array|subroutine|class|variable

                var varName = token.Value;

                //Is array
                if (PeekNext().Value == "[")
                {
                    token = GetNext(); // [

                    CompileExpression();

                    token = GetNext(); // ]

                    if (token.Value != "]")
                    {
                        throw new Exception("Missing ']'");
                    }

                    var variable = SymbolTable.GetSymbol(varName);

                    var segment = GetSegmentFor(variable.Kind);

                    _vm.Push(segment, variable.Index);
                    _vm.Arithmetic("add");
                    _vm.Pop("pointer", 1);
                    _vm.Push("that", 0);
                }

                //Subroutine
                else if (PeekNext().Value == "(")
                {
                    token = GetNext();

                    OutputToken(token);

                    CompileExpressionList();

                    token = GetNext();

                    if (token.Value != ")")
                    {
                        throw new Exception("Missing ')'");
                    }

                    OutputToken(token);
                }

                //Nested subrouting
                else if (PeekNext().Value == ".")
                {
                    var isMethod = true;

                    token = GetNext(); // .

                    token = GetNext(); // subroutine name

                    var subroutineName = token.Value;

                    var variable = SymbolTable.GetSymbol(varName);

                    if (variable == null)
                    {
                        isMethod = false;
                    }

                    token = GetNext(); // (

                    if (token.Value != "(")
                    {
                        throw new Exception("Missing '('");
                    }

                    var paramCount = 0;

                    if (isMethod)
                    {
                        //Need to push implicit this param
                        var segment = GetSegmentFor(variable.Kind);
                        _vm.Push(segment, variable.Index);
                        paramCount++;
                    }

                    paramCount += CompileExpressionList();

                    token = GetNext(); // )

                    if (token.Value != ")")
                    {
                        throw new Exception("Missing ')'");
                    }

                    if (isMethod)
                    {
                        _vm.Call(variable.Type + "." + subroutineName, paramCount);
                    }

                    //call function
                    else
                    {
                        _vm.Call(varName + "." + subroutineName, paramCount);
                    }
                }

                //variable
                else
                {
                    var variable = SymbolTable.GetSymbol(varName);

                    if (variable == null)
                    {
                        throw new Exception("Unknown identifier: " + varName);
                    }

                    var segment = GetSegmentFor(variable.Kind);
                    _vm.Push(segment, variable.Index);
                }
            }

            else if (PeekNext().Value == "(")
            {
                token = GetNext(); // (

                CompileExpression();

                token = GetNext(); // )

                if (token.Value != ")")
                {
                    throw new Exception("Missing ')'");
                }
            }

            else if (PeekNext().Value == "-" || PeekNext().Value == "~")
            {
                token = GetNext(); // op

                var operation = token.Value;

                CompileTerm();

                switch (operation)
                {
                    case "-":
                        _vm.Arithmetic("neg");
                        break;
                    case "~":
                        _vm.Arithmetic("not");
                        break;
                }
            }
        }

        private int CompileExpressionList()
        {
            var count = 1;

            //Empty Expression List
            if (PeekNext().Value == ")")
            {
                return 0;
            }

            CompileExpression();

            //additional expressions
            while (PeekNext().Value == ",")
            {
                var token = GetNext(); // ,

                CompileExpression();

                count++;
            }

            return count;
        }
    }
}