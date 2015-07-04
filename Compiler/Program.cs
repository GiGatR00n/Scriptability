using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Compiler;
using Irony.Parsing;

namespace Compiler
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

            if (args.Length >= 2)
            {
                ParseFile(args[0], args[1]);
            }
        }

        private static IEnumerable<string> ParseAllFiles(string path)
        {
            var temp = Directory.GetFiles(path).ToList();
            foreach (var dir in Directory.GetDirectories(path))
            {
                temp.AddRange(ParseAllFiles(dir));
            }
            return temp;
        }

        static void ParseFile(string source, string dest)
        {
            var grammar = new GSCGrammar();
            var parser = new Parser(grammar);
            var compiler = new ScriptCompiler(parser.Parse(File.ReadAllText(source)), dest);
            if (!compiler.Init())
                return;

            using (var writer = File.Create(dest))
            {
                writer.Write(compiler.Compiled, 0, compiler.Compiled.Length);
                writer.Dispose();
            }
        }
    }
}
