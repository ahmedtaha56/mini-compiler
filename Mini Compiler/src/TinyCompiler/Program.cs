using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace TinyCompiler
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        public static List<Token> TokenStream { get; private set; } = new List<Token>();
        public static Node treeRoot { get; private set; }
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WindowForm());
        }

        public static void Clear()
        {
            TokenStream.Clear();
            treeRoot = null;
            Errors.Clear();
        }
    }
}
