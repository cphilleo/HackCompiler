namespace HackCompiler.Tokens
{
    public interface ITokenRule
    {
        bool Matches(string text, int startIndex);
        Token GetToken();
    }
}