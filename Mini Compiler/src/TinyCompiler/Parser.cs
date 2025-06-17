using System;
using System.Collections.Generic;
using System.Linq;

namespace TinyCompiler
{
    public class ParseError : Exception { }


    public class Node
    {
        public List<Node> Children = new List<Node>();

        public string Name;

        public Node(string name) => Name = name;
    }

    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _inputPointer = 0;

        public Parser(List<Token> tokens) {
            _tokens = tokens;
        }

        public Node Parse()
        {
            _inputPointer = 0;

            if (!_tokens.Any())
            {
                return null;
            }

            return Program();
        }

        private Node Program() {
            Node n = new Node("Program");

            while (!IsAtEnd())
            {
                if (Peek2().lex == "main") break;

                try
                {
                    n.Children.Add(Func());
                }
                catch (ParseError)
                {
                    SynchronizeFunc();
                }
            }

            try
            {
                // Add main function
                if (IsAtEnd())
                {
                    Error(PreviousToken(), "expected main function");
                }
                else
                {
                    n.Children.Add(Func());
                }
            }
            catch (ParseError)
            {
                Errors.ReportError("parser: found fatal error... stopping");
            }

            return n;
        }

        private Node Func() {
            Node n = new Node("Function");

            n.Children.Add(FuncDecl());
            n.Children.Add(FuncBody());

            return n;
        }

        private Node FuncDecl() {
            Node n = new Node("Function Declaration");

            n.Children.Add(Datatype());
            n.Children.Add(Consume(TokenClass.Identifier));
            n.Children.Add(Consume(TokenClass.LeftParen, "expected '(' after function name"));

            Node parameters = new Node("Parameters");
            while (!Check(TokenClass.RightParen))
            {
                parameters.Children.Add(Param());

                if (!Check(TokenClass.RightParen))
                {
                    parameters.Children.Add(Consume(TokenClass.Comma, "expected ',' between parameters"));
                }
            }
            n.Children.Add(parameters);

            n.Children.Add(Consume(TokenClass.RightParen, "expected ')' after parameters"));

            return n;
        }

        private Node Param() {
            Node n = new Node("Parameter");

            n.Children.Add(Datatype());
            n.Children.Add(Consume(TokenClass.Identifier, "expected parameter name"));

            return n;
        }

        private Node FuncBody() {
            Node n = new Node("Function Body");

            n.Children.Add(Consume(TokenClass.LeftBrace, "expected '{' before function body"));
            n.Children.Add(Stmts());
            n.Children.Add(RetStmt());
            n.Children.Add(Consume(TokenClass.RightBrace, "expected '}' after function body"));

            return n;
        }

        private Node FuncCall() {
            Node n = new Node("Function Call");

            n.Children.Add(Consume(TokenClass.Identifier, "expected function name"));
            n.Children.Add(Consume(TokenClass.LeftParen, "expected '(' after function name"));

            Node args = new Node("Arguments");
            while (!Check(TokenClass.RightParen))
            {
                args.Children.Add(Arg());

                if (!Check(TokenClass.RightParen))
                {
                    args.Children.Add(Consume(TokenClass.Comma, "expected ',' between arguments"));
                }
            }
            n.Children.Add(args);

            n.Children.Add(Consume(TokenClass.RightParen, "expected ')' after arguments"));

            return n;
        }

        private Node Arg() {
            Node n = new Node("Argument");
            n.Children.Add(Expr());
            return n;
        }

        private Node Datatype() {
            Node n = new Node("Datatype");

            if (Match(TokenClass.Int, TokenClass.Float, TokenClass.String))
            {
                n.Children.Add(PreviousNode());
                return n;
            }

            throw Error(Peek(), "expected 'int', 'float', or 'string'");
        }

        private Node Stmt() {
            Node n = new Node("Statement");
            TokenClass type = (!IsAtEnd()) ? Peek().type : TokenClass.Undefined;

            try
            {
                switch (type)
                {
                    case TokenClass.Identifier:
                        n.Children.Add(AssignStmt());
                        n.Children.Add(Consume(TokenClass.Semicolon));
                        break;
                    case TokenClass.Read:
                        n.Children.Add(ReadStmt());
                        n.Children.Add(Consume(TokenClass.Semicolon));
                        break;
                    case TokenClass.Write:
                        n.Children.Add(WriteStmt());
                        n.Children.Add(Consume(TokenClass.Semicolon));
                        break;
                    case TokenClass.Repeat:
                        n.Children.Add(RepeatStmt());
                        break;
                    case TokenClass.If:
                        n.Children.Add(IfStmt());
                        break;
                    case TokenClass.Int:
                    case TokenClass.Float:
                    case TokenClass.String:
                        // Fall
                        n.Children.Add(DeclStmt());
                        n.Children.Add(Consume(TokenClass.Semicolon));
                        break;
                    default:
                        throw Error(Peek(), "expected statement");
                        break;
                }

                return n;
            }
            catch (ParseError)
            {
                Synchronize();
                return null;
            }
        }

        private Node Stmts() {
            Node n = new Node("Statements");
            TokenClass[] types = {
                TokenClass.Read, TokenClass.Write, TokenClass.Repeat, TokenClass.If,
                TokenClass.Int, TokenClass.Float, TokenClass.String, TokenClass.Identifier
            };

            while (Check(types))
            {
                n.Children.Add(Stmt());
            }

            return n;
        }

        private Node RepeatStmt() {
            Node n = new Node("Repeat Statement");

            n.Children.Add(Consume(TokenClass.Repeat));
            n.Children.Add(Stmts());
            n.Children.Add(Consume(TokenClass.Until));

            Node conds = new Node("Conditions");
            conds.Children.Add(LogicalOr());

            n.Children.Add(conds);

            return n;
        }

        private Node WriteStmt() {
            Node n = new Node("Write Statement");

            n.Children.Add(Consume(TokenClass.Write));

            if (Match(TokenClass.Endl))
            {
                n.Children.Add(PreviousNode());
            }
            else
            {
                n.Children.Add(Expr());
            }

            return n;
        }

        private Node ReadStmt() {
            Node n = new Node("Read Statement");

            n.Children.Add(Consume(TokenClass.Read));
            n.Children.Add(Consume(TokenClass.Identifier));

            return n;
        }

        private Node RetStmt() {
            Node n = new Node("Return Statement");

            n.Children.Add(Consume(TokenClass.Return));
            n.Children.Add(Expr());
            n.Children.Add(Consume(TokenClass.Semicolon));

            return n;
        }

        private Node IfStmt()  {
            Node n = new Node("If Statement");

            n.Children.Add(Consume(TokenClass.If));

            Node conds = new Node("Conditions");
            while (!Check(TokenClass.Then))
            {
                conds.Children.Add(LogicalOr());
            }
            n.Children.Add(conds);

            n.Children.Add(Consume(TokenClass.Then));
            n.Children.Add(Stmts());

            if (Check(TokenClass.ElseIf))
            {
                n.Children.Add(ElIfStmt());
            }

            if (Check(TokenClass.Else))
            {
                n.Children.Add(ElseStmt());
            }

            n.Children.Add(Consume(TokenClass.End));

            return n;
        }

        private Node ElIfStmt() {
            Node n = new Node("ElseIf Statement");

            n.Children.Add(Consume(TokenClass.ElseIf));

            Node conds = new Node("Conditions");
            while (!Check(TokenClass.Then))
            {
                conds.Children.Add(LogicalOr());
            }
            n.Children.Add(conds);

            n.Children.Add(Consume(TokenClass.Then));
            n.Children.Add(Stmts());

            if (Check(TokenClass.ElseIf))
            {
                n.Children.Add(ElIfStmt());
            }

            if (Check(TokenClass.Else))
            {
                n.Children.Add(ElseStmt());
            }

            return n;
        }

        private Node ElseStmt() {
            Node n = new Node("Else Statement");

            n.Children.Add(Consume(TokenClass.Else));
            n.Children.Add(Stmts());

            return n;
        }

        private Node Cond() {
            Node n = new Node("Condition");
            TokenClass[] condTokens = {
                TokenClass.Greater, TokenClass.Less,
                TokenClass.Equal, TokenClass.NotEqual
            };

            n.Children.Add(Consume(TokenClass.Identifier));
            if (Match(condTokens))
            {
                n.Children.Add(PreviousNode());
            }
            else
            {
                Error(Peek(), "expected '<', '>', '<>', or '=' operator");
            }
            n.Children.Add(Expr());

            return n;
        }

        private Node LogicalOr() {
            Node n = new Node("Logical OR");

            n.Children.Add(LogicalAnd());

            if (Check(TokenClass.Or))
            {
                n.Children.Add(Consume(TokenClass.Or));
                n.Children.Add(LogicalOr());
            }

            return n;
        }

        private Node LogicalAnd() {
            Node n = new Node("Logical AND");

            n.Children.Add(Cond());
            if (Check(TokenClass.And))
            {
                n.Children.Add(Consume(TokenClass.And));
                n.Children.Add(Cond());
            }

            return n;
        }

        private Node DeclStmt() {
            Node n = new Node("Declare Statement");

            n.Children.Add(Datatype());
            n.Children.Add(Decl());

            if (Check(TokenClass.Comma))
            {
                while (!Check(TokenClass.Semicolon))
                {
                    n.Children.Add(Consume(TokenClass.Comma));
                    n.Children.Add(Decl());
                }
            }

            return n;
        }

        private Node Decl() {
            Node n = new Node("Declaration");

            if (Check(TokenClass.Identifier) && Check2(TokenClass.Assign))
            {
                n.Children.Add(AssignStmt());
            }
            else
            {
                n.Children.Add(Consume(TokenClass.Identifier));
            }

            return n;
        }

        private Node AssignStmt()
        {
            Node n = new Node("Assignment Statement");

            n.Children.Add(Consume(TokenClass.Identifier));
            n.Children.Add(Consume(TokenClass.Assign));
            n.Children.Add(Expr());

            return n;
        }

        private Node Expr()
        {
            Node n = new Node("Expression");

            n.Children.Add(Term());
            if (Match(TokenClass.Minus, TokenClass.Plus))
            {
                n.Children.Add(PreviousNode());
                n.Children.Add(Expr());
            }

            return n;
        }

        private Node Factor()
        {
            Node n = new Node("Factor");

            if (Check(TokenClass.Identifier) && Check2(TokenClass.LeftParen))
            {
                n.Children.Add(FuncCall());
            }
            else if (Match(TokenClass.Number, TokenClass.StringLiteral, TokenClass.Identifier))
            {
                n.Children.Add(PreviousNode());
            }
            else if (Check(TokenClass.LeftParen))
            {
                n.Children.Add(Consume(TokenClass.LeftParen));
                n.Children.Add(Expr());
                n.Children.Add(Consume(TokenClass.RightParen));
            }
            else
            {
                Error(Peek(), "expected number, string, identifier, or (expression)");
            }

            return n;
        }

        private Node Term()
        {
            Node n = new Node("Term");

            n.Children.Add(Factor());
            if (Match(TokenClass.Multiply, TokenClass.Divide))
            {
                n.Children.Add(PreviousNode());
                n.Children.Add(Term());
            }

            return n;
        }

        private void Synchronize()
        {
            Advance();

            while (!IsAtEnd())
            {
                if (PreviousToken().type == TokenClass.Semicolon)
                {
                    return;
                }

                switch (Peek().type)
                {
                    case TokenClass.Int:
                    case TokenClass.Float:
                    case TokenClass.String:
                    case TokenClass.Identifier:
                    case TokenClass.Return:
                    case TokenClass.Read:
                    case TokenClass.Write:
                    case TokenClass.Repeat:
                    case TokenClass.If:
                        return;
                }

                Advance();
            }
        }

        private void SynchronizeFunc()
        {
            Advance();

            while (!IsAtEnd())
            {
                if (PreviousToken().type == TokenClass.RightBrace)
                {
                    return;
                }

                switch (Peek().type)
                {
                    case TokenClass.Int:
                    case TokenClass.Float:
                    case TokenClass.String:
                        return;
                }

                Advance();
            }
        }

        private Node ToNode(Token token)
        {
            Node n = new Node(token.type.ToString());
            n.Children.Add(new Node(token.lex));
            return n;
        }

        private Token Peek()
        {
            if (IsAtEnd()) return new Token { line = PreviousToken().line, type = TokenClass.Undefined };
            return _tokens[_inputPointer];
        }

        private Token Peek2()
        {
            if (_inputPointer + 1 >= _tokens.Count) return new Token { line = Peek().line, type = TokenClass.Undefined };
            return _tokens[_inputPointer + 1];
        }

        private bool IsAtEnd() => _inputPointer >= _tokens.Count;

        private Token PreviousToken() => _tokens[_inputPointer - 1];

        private Node PreviousNode() => ToNode(PreviousToken());

        private Token Advance()
        {
            if (!IsAtEnd()) _inputPointer++;
            return PreviousToken();
        }

        private bool Check(TokenClass type)
        {
            if (IsAtEnd()) return false;
            return Peek().type == type;
        }

        private bool Check(params TokenClass[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    return true;
                }
            }

            return false;
        }

        private bool Check2(params TokenClass[] types)
        {
            if (_inputPointer + 1 >= _tokens.Count)
            {
                return false;
            }

            foreach (var type in types)
            {
                if (Peek2().type == type)
                {
                    return true;
                }
            }

            return false;
        }

        private Node Consume(TokenClass type, string message)
        {
            if (Check(type)) return ToNode(Advance());
            throw Error(Peek(), message);
        }

        private Node Consume(TokenClass type)
        {
            return Consume(type, $"expected '{type.ToString().ToLowerInvariant()}'");
        }

        private ParseError Error(Token token, string message)
        {
            if (IsAtEnd())
            {
                Errors.ReportError(token.line, $"at end: {message}");
            }
            else
            {
                Errors.ReportError(token.line, $"at '{token.lex}': {message}");
            }

            return new ParseError();
        }

        private bool Match(params TokenClass[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }
    }
}
