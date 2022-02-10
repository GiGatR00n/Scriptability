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
        static int Main(string[] args)
        {
            if(args.Length == 1)
            {
                ParseSingleFile(args[0]);
            }
            else if (args.Length >= 3)
            {
                return ParseFile(args[0], args[1], args[2]);
            }

            return 0;
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

        static int ParseFile(string rel, string source, string dest)
        {
            var grammar = new GSCGrammar();
            var parser = new Parser(grammar);
            var compiler = new ScriptCompiler(parser.Parse(File.ReadAllText(source)), dest);

            int result = (compiler.Init(rel) ? 1 : 0);

            using (var writer = File.Create(dest))
            {
                writer.Write(compiler.Compiled, 0, compiler.Compiled.Length);
                writer.Dispose();
            }

            return result;
        }

        static void ParseSingleFile(string file)
        {
            string dest = file.Substring(file.LastIndexOfAny(new char[] { '/', '\\' }) + 1);
            string destPath = Path.GetFileNameWithoutExtension(file) + "-compiled.gsc";

            var grammar = new GSCGrammar();
            var parser = new Parser(grammar);
            var compiler = new ScriptCompiler(parser.Parse(File.ReadAllText(file)), dest);

            if (compiler.Init(dest.Substring(0, dest.IndexOf(".")), true))
            {
                using (var writer = File.Create(destPath))
                {
                    writer.Write(compiler.Compiled, 0, compiler.Compiled.Length);
                    writer.Dispose();
                }
            }
        }
    }
}
