using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace new_MachineMenimazer
{

    class Machine
    {
        private List<State> _states;
        public MachineType MachineType { get; }
        public int ZCount { get; }
        public int StatesCount { get; }
        public List<State> States { get => _states; }

        public Machine(MachineType machineType, List<State> states, int zCount)
        {
            MachineType = machineType;
            ZCount = zCount;
            _states = states;
            StatesCount = _states.Count;
        }

        public Machine(TextReader input)
        {
            string? machineTypeStr = input.ReadLine();
            if (machineTypeStr == null)
                throw new ArgumentException("Invalid argument");

            machineTypeStr = machineTypeStr.ToLower();
            MachineType machineType;

            StatesCount = int.Parse(ReadArg(input));
            
            _states = new(StatesCount);
            for (int i = 0; i < StatesCount; i++)
                _states.Add(new State());
            
            ZCount = int.Parse(ReadArg(input));
            
            int.Parse(ReadArg(input));


            if (machineTypeStr == "ml")
            {
                machineType = MachineType.Milli;
                ReadMilliMachine(input);
            }
            else if (machineTypeStr == "mr")
            {
                machineType = MachineType.Moore;
                ReadMooreMachine(input);
            }
            else
                throw new ArgumentException("invalid machine type");

            MachineType = machineType;

        }

        #region Read

        private void ReadMooreMachine(TextReader input)
        {
            string[] @out = ReadArgs(input);
            for (int i = 0; i < ZCount; ++i)
            {
                string[] trasition = ReadArgs(input);
                for (int j = 0; j < StatesCount; ++j)
                {
                    _states[j].Transitions.Add(new(int.Parse(trasition[j]) - 1, int.Parse(@out[j])));
                }
            }
        }

        private void ReadMilliMachine(TextReader input)
        {
            for (int i = 0; i < ZCount; i++)
            {
                string[] trasition = ReadArgs(input);
                string[] @out = ReadArgs(input);
                for (int j = 0; j < StatesCount; ++j)
                {
                    _states[j].Transitions.Add(new Cell(int.Parse(trasition[j]) - 1, int.Parse(@out[j])));
                }
            }

        }
        private string[] ReadArgs(TextReader input)
        {
            return (Regex.Replace(ReadArg(input), "[A-Za-z]", "")).Split(" ");
        }

        private string ReadArg(TextReader input)
        {
            string? arg = input.ReadLine();
            if (arg == null)
                throw new ArgumentException("Ivalid arguments");

            return arg;
        }


        #endregion

        #region Write

        public void Write(TextWriter output)
        {
            if (MachineType == MachineType.Milli)
                WriteMilli(output);
            else if (MachineType == MachineType.Moore)
                WriteMoore(output);
        }
        private void WriteMilli(TextWriter output)
        {
            for(int i = 0; i < ZCount; ++i)
            {
                WriteStates(output, i);
                WriteOut(output, i);
            }
        }
        private void WriteMoore(TextWriter output)
        {
            WriteOut(output);
            for (int i = 0; i < ZCount; ++i)
            {
                WriteStates(output, i);
            }
        }

        private void WriteStates(TextWriter output, int z = 0)
        {
            for(int i = 0; i < StatesCount; ++i)
            {
                var currState = States[i].Transitions[z].To + 1;
                output.Write("S{0} ", currState);
            }
            output.WriteLine();
        }

        private void WriteOut(TextWriter output, int z = 0)
        {
            for (int i = 0; i < StatesCount; ++i)
            {
                var currState = States[i].Transitions[z].Out;
                output.Write("Y{0} ", currState);
            }
            output.WriteLine();
        }

        #endregion

        #region Minimization

        public Machine Minimaze()
        {
            var tempMachine = new List<State>(_states);
            int groupCount = 0;

            if (SetGroup(tempMachine, state => state.Out, ref groupCount))
                return this;

            while (!SetGroup(tempMachine, state => tempMachine[state.To].Group, ref groupCount)) ;

            return GetMinimazedMachine(tempMachine);
        }

        private bool SetGroup(List<State> machine, Func<Cell, int> getter, ref int groupCount)
        {
            bool isMinimazed = true;
            List<List<int>> groupsValue = new List<List<int>>();
            int[] newGroups = new int[machine.Count];
            for (int i = 0; i < machine.Count; ++i)
            {
                List<int> currGroup = new List<int>();
                currGroup.Add(machine[i].Group);
                foreach (var transition in machine[i].Transitions)
                {
                    currGroup.Add(getter(transition));
                }

                var group = groupsValue.Find((x => x.SequenceEqual(currGroup)));
                int index = 0;
                if (group == null || (index = groupsValue.IndexOf(group)) == -1)
                {
                    groupsValue.Add(currGroup);
                    index = groupsValue.Count - 1;
                }

                newGroups[i] = index;

            }

            if (groupCount != groupsValue.Count)
                isMinimazed = false;
            groupCount = groupsValue.Count;

            for (int i = 0; i < machine.Count; ++i)
            {
                var temp = machine[i];
                temp.Group = newGroups[i];
                machine[i] = temp;
            }

            return isMinimazed;
        }

        private Machine GetMinimazedMachine(List<State> machine)
        {
            var minimazedMachine = new List<State>();

            List<int> indexes = new();
            foreach (var state in machine)
            {
                if (indexes.Exists(x => x == state.Group))
                    continue;

                indexes.Add(state.Group);
                minimazedMachine.Add(state);
            }

            for (int i = 0; i < minimazedMachine.Count; ++i)
            {
                for (int j = 0; j < minimazedMachine[i].Transitions.Count; ++j)
                {
                    var temp = minimazedMachine[i].Transitions[j];
                    temp.To = indexes.Find(x => x == machine[temp.To].Group);
                    minimazedMachine[i].Transitions[j] = temp;
                }
            }

            return new(MachineType, minimazedMachine, ZCount);
        }

        #endregion

    }

    enum MachineType
    {
        Milli,
        Moore
    }

    internal struct State
    {
        public int Group = 0;
        public List<Cell> Transitions = new();
    }

    internal struct Cell
    {
        public int To;
        public int Out;

        public Cell(int to, int @out)
        {
            To = to;
            Out = @out;
        }
    }
}
