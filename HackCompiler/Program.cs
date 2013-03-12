using System;
using System.Collections.Generic;
using System.IO;

namespace HackCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: HackCompiler {file}|{folder}");
                return;
            }

            var path = args[0];
            var files = new List<string>();

            //directory
            if (!path.EndsWith(".jack"))
            {
                files.AddRange(Directory.GetFiles(path, "*.jack"));
            }

            //single file
            else
            {
                files.Add(path);
            }

            foreach (var file in files)
            {
                Console.WriteLine("Processing file: " + file);
                Console.WriteLine("Tokenizing...");

                var tokens = Tokenizer.Tokenize(file);

                Console.WriteLine("Found {0} tokens", tokens.Count);

//                foreach (var token in tokens)
//                {
//                    Console.WriteLine("Type: {0}, Value: {1}", token.Type, token.Value);
//                }

                Console.WriteLine("Compiling...");
                var compiler = new Compiler(tokens);

                var outFile = file.Replace(".jack", ".vm");

                //try
                //{
                    string output = compiler.Compile();
                    File.WriteAllText(outFile, output);
                    Console.WriteLine(output);

                    //reset symbol table for next file
                    SymbolTable.Reset();
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine("Compilation Error: " + ex.Message);
                //}
            }
        }
    }
}
