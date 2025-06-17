using System.Collections.Generic;

namespace TinyCompiler
{
    public enum TokenClass
    {
        Undefined,

        // Single character tokens
        LeftBrace, RightBrace, LeftParen, RightParen,
        Minus, Plus, Multiply, Divide,

        // One or two character tokens
        Assign,
        Less, Greater, Equal, NotEqual, And, Or, Comma, Semicolon,

        // Keywords
        Int, Float, String,
        Read, Write, Repeat, Until, If, ElseIf, Else, End, Endl, Then, Return,

        // Literals
        Identifier, Number, StringLiteral,
    }

    public class Token
    {
        public string lex;
        public TokenClass type;
        public int line;
    }

    public class Scanner
    {
        private readonly static Dictionary<string, TokenClass> _keywords = new Dictionary<string, TokenClass>{
            {"int", TokenClass.Int},
            {"float", TokenClass.Float},
            {"string", TokenClass.String},
            {"read", TokenClass.Read},
            {"write", TokenClass.Write},
            {"repeat", TokenClass.Repeat},
            {"until", TokenClass.Until},
            {"if", TokenClass.If},
            {"elseif", TokenClass.ElseIf},
            {"else", TokenClass.Else},
            {"end", TokenClass.End},
            {"endl", TokenClass.Endl},
            {"then", TokenClass.Then},
            {"return", TokenClass.Return},
        };

        private readonly List<Token> _tokens = new List<Token>();
        private readonly string _sourceCode;
        private int _start = 0;
        private int _current = 0;
        private int _linenumber = 1;

        public Scanner(string sourceCode) {
            _sourceCode = sourceCode;
        }

        public List<Token> Scan()
        {
            _tokens.Clear();

            _linenumber = 1;
            _start = 0;
            _current = 0;

            while (!IsEOF())
            {
                _start = _current;
                char c = Read();

                switch (c)
                {
                    case '{': AddToken(TokenClass.LeftBrace); break;
                    case '}': AddToken(TokenClass.RightBrace); break;
                    case '(': AddToken(TokenClass.LeftParen); break;
                    case ')': AddToken(TokenClass.RightParen); break;
                    case ';': AddToken(TokenClass.Semicolon); break;
                    case ',': AddToken(TokenClass.Comma); break;
                    case '-': AddToken(TokenClass.Minus); break;
                    case '+': AddToken(TokenClass.Plus); break;
                    case '*': AddToken(TokenClass.Multiply); break;
                    case '/':
                        if (Match('*'))
                            ReadComment();
                        else
                            AddToken(TokenClass.Divide);
                        break;
                    case ':':
                        if (!Match('='))
                            goto default;
                        AddToken(TokenClass.Assign);
                        break;
                    case '=': AddToken(TokenClass.Equal); break;
                    case '>': AddToken(TokenClass.Greater); break;
                    case '<': AddToken(Match('>') ? TokenClass.NotEqual : TokenClass.Less); break;
                    case '&':
                        if (!Match('&'))
                            goto default;
                        AddToken(TokenClass.And);
                        break;
                    case '|':
                        if (!Match('|'))
                            goto default;
                        AddToken(TokenClass.Or);
                        break;
                    case '"':
                        ReadString();
                        break;

                    // Fall through
                    case '\r':
                    case '\t':
                    case ' ':
                        break;

                    case '\n':
                        _linenumber++;
                        break;

                    default:
                        if (char.IsDigit(c))
                        {
                            ReadNumber();
                        }
                        else if (char.IsLetter(c))
                        {
                            ReadIdentifier();
                        }
                        else
                        {
                            Errors.ReportError(_linenumber, $"unexpected lexeme '{c}'");
                        }
                        break;
                }
            }

            return _tokens;
        }

        private void ReadIdentifier()
        {
            while (char.IsLetterOrDigit(Peek()))
                Read();

            string lex = _sourceCode.Substring(_start, _current - _start);
            AddToken(_keywords.ContainsKey(lex) ? _keywords[lex] : TokenClass.Identifier);
        }

        private void ReadNumber()
        {
            while (char.IsDigit(Peek()))
                Read();

            if ((Peek() == '.') && char.IsDigit(PeakNext()))
            {
                // Read '.'
                Read();

                while (char.IsDigit(Peek()))
                    Read();
            } else if (char.IsLetter(Peek())) {
                Read();
                Errors.ReportError(_linenumber, "illegal identifier");
                return;
            }

            AddToken(TokenClass.Number);
        }

        private void ReadString()
        {
            char prev = '\0';
            while (!IsEOF())
            {
                if ((Peek() == '"') && (prev != '\\'))
                    break;

                // No multiline strings
                if (Peek() == '\n')
                    break;

                prev = Read();
            }

            if (Match('"'))
                AddToken(TokenClass.StringLiteral);
            else
                Errors.ReportError(_linenumber, "unterminated string, expected '\"'");
        }

        private void ReadComment()
        {
            while (!IsEOF())
            {
                if ((Peek() == '*') && (PeakNext() == '/'))
                {
                    Read();
                    break;
                }

                if (Read() == '\n')
                    _linenumber++;

            }

            if (Peek() == '/')
                Read();
            else
                Errors.ReportError(_linenumber, "unterminated comment, expected '*/'");
        }

        private void AddToken(TokenClass type)
        {
            Token token = new Token
            {
                lex = _sourceCode.Substring(_start, _current - _start),
                type = type,
                line = _linenumber,
            };

            _tokens.Add(token);
        }

        private char Read() => _sourceCode[_current++];

        private char Peek()
        {
            if (IsEOF())
                return '\0';

            return _sourceCode[_current];
        }

        private char PeakNext()
        {
            if (_current + 1 >= _sourceCode.Length)
                return '\0';

            return _sourceCode[_current + 1];
        }

        private bool Match(char c)
        {
            if (IsEOF())
                return false;

            if (_sourceCode[_current] != c)
                return false;

            _current++;
            return true;
        }

        private bool IsEOF() => (_current >= _sourceCode.Length);
    }
}
