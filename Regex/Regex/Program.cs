using System;
using System.IO;

namespace Regex
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TextReader input = File.OpenText("in.txt");
            var regex = input.ReadLine();
            if (regex == null)
                regex = string.Empty;
            input.Close();

            var firstNode = RegexParser.Parse(regex);
            var dataList = RegexParser.ToStringList(firstNode);

            TextWriter output = File.CreateText("out.txt");

            foreach (var data in dataList)
            {
                output.WriteLine(data);
            }
            output.Close();
        }
    }
}
