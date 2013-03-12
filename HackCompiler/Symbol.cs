namespace HackCompiler
{
    public class Symbol
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public SymbolKind Kind { get; set; }
        public int Index { get; set; }
    }

    public enum SymbolKind
    {
        Static,
        Field,
        Argument,
        Variable
    }
}