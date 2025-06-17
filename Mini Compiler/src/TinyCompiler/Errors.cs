using System.Collections.Generic;
using System.Linq;

namespace TinyCompiler
{
    public static class Errors
    {
        private static readonly List<string> ErrorList = new List<string>();

        public static List<string> GetAll() => ErrorList;

        public static int Count() => ErrorList.Count();

        public static bool HasError() => ErrorList.Any();

        public static void Clear() => ErrorList.Clear();

        public static void ReportError(string msg) => ErrorList.Add(msg);

        public static void ReportError(int lineNumber, string msg)
        {
            ReportError($"line:{lineNumber}: error: {msg}");
        }
    }
}
