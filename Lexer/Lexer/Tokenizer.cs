using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    internal enum TokenType
    {
        Unexpected,
        Number,
        Identifier,
        LeftParen,
        RightParen,
        LeftSquare,
        RightSquare,
        LeftCurly,
        RightCurly,
        LessThan,
        GreaterThan,
        Equal,
        Plus,
        Minus,
        Multiplication,
        OneLineComment,
        Hash,
        Dot,
        Comma,
        Colon,
        Semicolon,
        SingleQuote,
        DoubleQuote,
        Comment,
        Or,
        And,
        End,
        ObjectType,
        AccessType,
        Appropriation,
        TypeName,
        KeyWord,
        Literal,
        OpenComment,
        CloseComment
    }

    internal class TokenDefinition
    {
        private string[] _defenitions;
        private readonly TokenType _returnsToken;

        public TokenDefinition(TokenType returnsToken, params string[] defenition)
        {
            _defenitions = defenition;
            _returnsToken = returnsToken;
        }

        public TokenMatch Match(string inputString)
        {
            bool IsMatch = false;
            TokenType type = TokenType.Unexpected;
            foreach (var defenition in _defenitions)
            {
                if (inputString == defenition)
                {
                    IsMatch = true;
                    type = _returnsToken;
                    break;
                }
            }

            return new TokenMatch()
            {
                IsMatch = IsMatch,
                TokenType = type,
                Value = inputString
            };

        }
    }

    internal class TokenMatch
    {
        public bool IsMatch { get; set; }
        public TokenType TokenType { get; set; }
        public string Value { get; set; } = String.Empty;
    }

    internal class Token
    {
        public Token(TokenType tokenType)
        {
            TokenType = tokenType;
            Value = string.Empty;
        }

        public Token(TokenType tokenType, string value,
            int columnPos, int linePos)
        {
            TokenType = tokenType;
            Value = value;
            ColumnPos = columnPos;
            LinePos = linePos;
        }

        public TokenType TokenType { get; set; }
        public string Value { get; set; }
        public int ColumnPos { get; set; }
        public int LinePos { get; set; }

        public Token Clone()
        {
            return new Token(TokenType, Value,
                ColumnPos, LinePos);
        }
    }

    internal class Tokenizer
    {
        private List<TokenDefinition> _tokenDefinitions;
        private bool _isComment = false;
        private StringBuilder _comment = new();
        private int _commentStartLinePos = 0;
        private int _commentStartPos = 0;

        public Tokenizer()
        {
            _tokenDefinitions = new List<TokenDefinition>
                {
                    new TokenDefinition(TokenType.LeftParen, "("),
                    new TokenDefinition(TokenType.RightParen, ")"),
                    new TokenDefinition(TokenType.LeftSquare, "["),
                    new TokenDefinition(TokenType.RightSquare, "]"),
                    new TokenDefinition(TokenType.LeftCurly, "{"),
                    new TokenDefinition(TokenType.RightCurly, "}"),
                    new TokenDefinition(TokenType.LessThan, ">"),
                    new TokenDefinition(TokenType.GreaterThan, "<"),
                    new TokenDefinition(TokenType.Equal, "=="),
                    new TokenDefinition(TokenType.Plus, "+"),
                    new TokenDefinition(TokenType.Minus, "-"),
                    new TokenDefinition(TokenType.Multiplication, "*"),
                    new TokenDefinition(TokenType.OneLineComment, "//"),
                    new TokenDefinition(TokenType.Hash, "#"),
                    new TokenDefinition(TokenType.Dot, "."),
                    new TokenDefinition(TokenType.Comma, ","),
                    new TokenDefinition(TokenType.Semicolon, ";"),
                    new TokenDefinition(TokenType.Colon, ":"),
                    new TokenDefinition(TokenType.SingleQuote, "'"),
                    new TokenDefinition(TokenType.DoubleQuote, "\""),
                    new TokenDefinition(TokenType.Or, "||"),
                    new TokenDefinition(TokenType.And, "&&"),
                    new TokenDefinition(TokenType.ObjectType, "struct", "class"),
                    new TokenDefinition(TokenType.AccessType, "public", "private", "internal"),
                    new TokenDefinition(TokenType.TypeName, "int", "double", "string", "char", "void"),
                    new TokenDefinition(TokenType.KeyWord, "if", "var"),
                    new TokenDefinition(TokenType.OpenComment, "/*"),
                    new TokenDefinition(TokenType.CloseComment, "*/"),
                    new TokenDefinition(TokenType.Dot, "."),
                };
        }

        public List<Token> Tokenize(string[] line)
        {
            var tokens = new List<Token>();
            int linePos = 1;
            foreach (var token in line)
            {

                for (int i = 0; i < token.Length; ++i)
                {
                    if (_isComment)
                    {
                        if (token[i] == '*' && i + 1 < token.Length)
                            if (token[i + 1] == '/')
                            {
                                _isComment = false;
                                tokens.Add(new(TokenType.Comment, _comment.ToString(),
                                    _commentStartPos, _commentStartLinePos));
                                tokens.Add(new(TokenType.CloseComment, token[i..(i + 2)], i, linePos));
                                ++i;
                                _comment.Clear();
                                continue;
                            }
                        _comment.Append(token[i]);
                        continue;
                    }

                    if (char.IsWhiteSpace(token[i]))
                        continue;

                    StringBuilder word = new();
                    bool wasIdentifierFound = false;
                    int startWordPos = i;
                    while (i < token.Length && (char.IsLetterOrDigit(token[i]) || token[i] == '_'))
                    {
                        wasIdentifierFound = true;
                        word.Append(token[i++]);
                    }

                    TokenMatch tokenMatch = new() { IsMatch = false, TokenType = TokenType.Unexpected };
                    if (!wasIdentifierFound)
                    {
                        while (i < token.Length && !
                            (char.IsLetterOrDigit(token[i]) || token[i] == '_' ||
                            char.IsWhiteSpace(token[i])))
                        {
                            word.Append(token[i++]);
                            tokenMatch = FindMatch(word.ToString());
                            if (tokenMatch.IsMatch)
                                break;
                        }
                    }
                    else
                        tokenMatch = FindMatch(word.ToString());

                    tokens.Add(new(tokenMatch.TokenType, tokenMatch.Value, startWordPos, linePos));

                    if (tokenMatch.TokenType == TokenType.OpenComment)
                    {
                        _isComment = true;
                        _commentStartLinePos = linePos;
                        _commentStartPos = i;
                    }

                    if (tokenMatch.TokenType == TokenType.SingleQuote ||
                       tokenMatch.TokenType == TokenType.DoubleQuote)
                    {
                        word = new();
                        while (i < token.Length && token[i].ToString() != tokenMatch.Value)
                        {
                            word.Append(token[i++]);
                        }
                        tokens.Add(new(TokenType.Literal, word.ToString(), i, 0));
                        if (i < token.Length)
                            tokens.Add(new(tokenMatch.TokenType, tokenMatch.Value, i, linePos));
                    }

                    if (tokenMatch.TokenType == TokenType.OneLineComment)
                    {
                        tokens.Add(new(TokenType.Comment, token[i..], i, linePos));
                        break;
                    }
                    --i;
                }
                ++linePos;
            }

            return tokens;
        }
        private TokenMatch FindMatch(string text)
        {
            foreach (var tokenDefinition in _tokenDefinitions)
            {
                var tockenMatch = tokenDefinition.Match(text);
                if (tockenMatch.IsMatch)
                    return tockenMatch;
            }
            TokenMatch match = MathIdentifier(text);
            if (match.IsMatch)
                return match;

            match = MathNumber(text);
            if (match.IsMatch)
                return match;

            return new TokenMatch()
            {
                IsMatch = false,
                TokenType = TokenType.Unexpected,
                Value = text
            };
        }

        private TokenMatch MathNumber(string text)
        {
            double number = 0;
            if (double.TryParse(text, out number))
                return new TokenMatch()
                {
                    IsMatch = true,
                    TokenType = TokenType.Number,
                    Value = text
                };
            else
                return new TokenMatch
                {
                    IsMatch = false,
                    TokenType = TokenType.Unexpected,
                    Value = text
                };
        }

        private TokenMatch MathIdentifier(string text)
        {
            bool isMatch = true;
            for (int i = 0; i < text.Length; ++i)
            {
                if (char.IsDigit(text[i]))
                {
                    if (i == 0)
                    {
                        isMatch = false;
                        break;
                    }
                    else
                        continue;
                }

                if (!(char.IsLetter(text[i]) || text[i] == '_'))
                {
                    isMatch = false;
                    break;
                }
            }

            if (isMatch)
                return new TokenMatch()
                {
                    IsMatch = isMatch,
                    TokenType = TokenType.Identifier,
                    Value = text
                };
            else
                return new TokenMatch()
                {
                    IsMatch = isMatch,
                    TokenType = TokenType.Unexpected,
                    Value = text
                };
        }

    }
}

