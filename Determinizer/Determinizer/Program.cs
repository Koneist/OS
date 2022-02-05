using System;
using System.IO;

namespace Determinizer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TextReader input = File.OpenText("in.txt");
            var machine = new Machine(input).Determine();
            TextWriter output = File.CreateText("out.txt");
            machine.Write(output);
            output.Close();
            input.Close();
        }
    }
}
