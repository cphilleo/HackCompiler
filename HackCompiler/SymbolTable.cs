using System.Collections.Generic;
using System.Linq;

namespace HackCompiler
{
    public static class SymbolTable
    {
        private static readonly Dictionary<string, Symbol> _classScope = new Dictionary<string, Symbol>();
        private static readonly Dictionary<string, Symbol> _subroutineScope = new Dictionary<string, Symbol>();

        public static void Define(string name, string type, SymbolKind kind)
        {
            var symbol = new Symbol
                         {
                             Name = name,
                             Type = type,
                             Kind = kind
                         };

            if (kind == SymbolKind.Static || kind == SymbolKind.Field)
            {
                symbol.Index = _classScope.Count(x => x.Value.Kind == kind);
                _classScope.Add(name, symbol);
            }

            else
            {
                symbol.Index = _subroutineScope.Count(x => x.Value.Kind == kind);
                _subroutineScope.Add(name, symbol);
            }
        }

        public static void Reset()
        {
            _subroutineScope.Clear();
            _classScope.Clear();
        }

        public static void StartNewSubroutine()
        {
            _subroutineScope.Clear();
        }

        public static Symbol GetSymbol(string name)
        {
            if (_subroutineScope.ContainsKey(name))
            {
                return _subroutineScope[name];
            }

            if (_classScope.ContainsKey(name))
            {
                return _classScope[name];
            }

            return null;
        }

        public static int GetCount(SymbolKind kind)
        {
            if (kind == SymbolKind.Static || kind == SymbolKind.Field)
            {
                return _classScope.Count(x => x.Value.Kind == kind);
            }

            return _subroutineScope.Count(x => x.Value.Kind == kind);
        }
    }
}