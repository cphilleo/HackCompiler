using System;
using System.Collections.Generic;
using System.IO;
using HackCompiler.Tokens;

namespace HackCompiler
{
    public static class Tokenizer
    {
        public static List<Token> Tokenize(string file)
        {
            var tokens = new List<Token>();
            var input = File.ReadAllText(file);
            var rules = new List<ITokenRule>
                        {
                            new SingleLineCommentRule(),
                            new MultilineCommentRule(),
                            new WhitespaceRule(),
                            new SymbolRule(),
                            new IntegerConstantRule(),
                            new StringConstantRule(),
                            new KeywordRule(),
                            new IdentifierRule()
                        };

            int currentIndex = 0;
            Token currentToken = null; 

            while (currentIndex < input.Length)
            {
                foreach (var rule in rules)
                {
                    if (rule.Matches(input, currentIndex))
                    {
                        currentToken = rule.GetToken();
                        break;
                    }
                }

                if (currentToken == null)
                {
                    throw new Exception("Unrecognized token");
                }

                if (currentToken.Type != TokenType.Ignored)
                {
                    tokens.Add(currentToken);
                }

                currentIndex += currentToken.Value.Length;
                currentToken = null;
            }

            return tokens;
        }
    }
}