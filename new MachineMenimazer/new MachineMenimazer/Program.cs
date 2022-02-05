using new_MachineMenimazer;

TextReader input = File.OpenText("input.txt");
TextWriter output = File.CreateText("output.txt");

Machine machine = new(input);
input.Close();

machine.Minimaze().Write(output);
output.Close();
