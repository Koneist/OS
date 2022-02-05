using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Lexer
{
    internal class Program
    {
        static void WriteTokens(List<Token> tokens, TextWriter output)
        {
            foreach (var token in tokens)
            {
                output.WriteLine("|{0}| {1}: line:{2} column:{3}",
                    token.Value, token.TokenType.ToString(), token.LinePos, token.ColumnPos);
            }
        }

        static void Main(string[] args)
        {
            TextReader input = File.OpenText("in.txt");
            var linesStr = input.ReadToEnd();

            var lines = linesStr.Split("\r\n");

            Tokenizer tokenizer = new();
            var tokens = tokenizer.Tokenize(lines);

            TextWriter output = File.CreateText("out.txt");
            WriteTokens(tokens, output);
            output.Close();
        }
    }
}
