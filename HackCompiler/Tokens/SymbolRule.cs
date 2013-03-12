﻿using System.Text.RegularExpressions;

namespace HackCompiler.Tokens
{
    public class SymbolRule : ITokenRule
    {
        private Token _token;

        public bool Matches(string text, int startIndex)
        {
            var regex = new Regex(@"\{|\}|\(|\)|\[|\]|\.|,|;|\+|-|\*|/|&|\||<|>|=|~");
            var match = regex.Match(text, startIndex);

            if (match.Success && (match.Index - startIndex == 0))
            {
                _token = new Token(match.Value, TokenType.Symbol);
                return true;
            }

            return false;
        }

        public Token GetToken()
        {
            return _token;
        }
    }
}