using System.Collections.Generic;

namespace TinyCompiler
{
    public static class Compiler
    {
        public static List<Token> TokenStream = new List<Token>();
        public static Node treeRoot;

        public static void Compile(string sourceCode)
        {
            Clear();

            //Scanner
            Scanner scanner = new Scanner(sourceCode);
            TokenStream = scanner.Scan();
            if (Errors.HasError()) {
                Errors.ReportError($"========== compile: {Errors.Count()} lex error ==========");
                return;
            }

            //Parser
            Parser parser = new Parser(TokenStream);
            treeRoot = parser.Parse();
            if (Errors.HasError()) {
                Errors.ReportError($"========== compile: {Errors.Count()} parse error ==========");
                return;
            }
        }

        public static void Clear()
        {
            Errors.Clear();
            TokenStream.Clear();
            treeRoot = null;
        }
    }
}
