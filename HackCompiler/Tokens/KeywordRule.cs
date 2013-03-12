using System.Text.RegularExpressions;

namespace HackCompiler.Tokens
{
    public class KeywordRule : ITokenRule
    {
        private Token _token;

        public bool Matches(string text, int startIndex)
        {
            var regex = new Regex(@"class\s+|constructor\s+|function\s+|method\s+|field\s+|static\s+|var\s+|int\s+|char\s+|boolean\s+|void\s+|true|false|null|this|let\s+|do\s+|if|else|while|return\s+");
            var match = regex.Match(text, startIndex);

            if (match.Success && (match.Index - startIndex == 0))
            {
                _token = new Token(match.Value.Trim(), TokenType.Keyword);
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