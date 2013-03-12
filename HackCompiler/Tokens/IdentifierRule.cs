﻿using System.Text.RegularExpressions;

namespace HackCompiler.Tokens
{
    public class IdentifierRule : ITokenRule
    {
        private Token _token;

        public bool Matches(string text, int startIndex)
        {
            var regex = new Regex(@"\w+");
            var match = regex.Match(text, startIndex);

            if (match.Success && (match.Index - startIndex == 0))
            {
                _token = new Token(match.Value, TokenType.Identifier);
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