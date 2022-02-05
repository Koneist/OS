using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Determinizer
{
    internal class Machine
    {
        private ReadType _readType;
        private Dictionary<string ,List<Transition>> _states = new();

        public Machine(Dictionary<string, List<Transition>> states, ReadType type)
        {
            _states = states;
            _readType = type;
        }


        public Machine(TextReader input)
        {
            var type = input.ReadLine();
            if (type == null)
                return;

            type = type.ToLower();
            if(type == "left")
                _readType = ReadType.Left;
            else if(type == "right")
                _readType = ReadType.Right;

            if (_readType == ReadType.Left)
                ReadLeft(input);
            else if (_readType == ReadType.Right)
                ReadRight(input);


        }

        #region read
        private void ReadRight(TextReader input)
        {
            string lineStr;

            while ((lineStr = input.ReadLine()) != null)
            {
                var line = lineStr.Split("->");

                var transitions = line[1].Split('|');
                List<Transition> transitionsList = new();
                for(int j = 0; j < transitions.Length; ++j)
                {
                    if (transitions[j].Length == 1)
                        transitions[j] = transitions[j] + 'H';

                    Transition transition = 
                        new(transitions[j][1].ToString(), transitions[j][0]);
                    transitionsList.Add(transition);
                }
                transitionsList.Sort(
                    (x1, x2) => { return x1.Out < x2.Out ? x1.Out > x2.Out ? -1 : 0 : 1; });
                _states.Add(line[0] ,transitionsList);
            }

            _states.Add("H", new());
        }

        private void ReadLeft(TextReader input)
        {
            string lineStr;

            while((lineStr = input.ReadLine()) != null)
            {
                var line = lineStr.Split("->");
                
                var transitions = line[1].Split('|');
                for(int j = 0; j < transitions.Length; ++j)
                {
                    if (transitions[j].Length == 1)
                        transitions[j] = 'H' + transitions[j];

                    string from = transitions[j][0].ToString();
                    if (!_states.ContainsKey(from))
                        _states[from] = new();

                    Transition transition = new(line[0], transitions[j][1]);
                    _states[from].Add(transition);
                }

            }

            foreach(var state in _states)
            {
                state.Value.Sort(
                    (x1, x2) => { return x1.Out < x2.Out ? x1.Out > x2.Out ? -1 : 0 : 1; });
            }
        }

        #endregion

        #region determine
        public Machine Determine()
        {
            Dictionary<string ,List<Transition>> states = new();
            Queue<string> newStates = new();

            if(_readType == ReadType.Right)
                newStates.Enqueue("S");
            else
                newStates.Enqueue("H");
            
            while(newStates.Count > 0)
            {
                List<Transition> allTransitions;
                var state = newStates.Dequeue();
                if (states.ContainsKey(state))
                    continue;
                if(!_states.TryGetValue(state, out allTransitions))
                {
                    allTransitions = new();
                    for(int i = 0; i < state.Length; ++i)
                    {
                        allTransitions = allTransitions.Concat(_states[state[i].ToString()]).ToList();
                    }
                }

                var result = allTransitions.GroupBy(x => x.Out);

                List<Transition> newTransitions = new();
                foreach (var transitions in result)
                {
                    string newTo = string.Empty;
                    foreach(var transition in transitions)
                    {
                        newTo += transition.To;
                    }
                    

                    newTo =  new(newTo.Distinct().ToArray());
                    newTo = string.Concat(newTo.OrderBy(x => x).ToArray());
                    if (!states.ContainsKey(newTo))
                        newStates.Enqueue(newTo);

                    newTransitions.Add(new(newTo, transitions.Key));
                }

                states.Add(state, newTransitions);
            }

            return new Machine(states, _readType);
        }

        #endregion

        #region write

        public void Write(TextWriter output)
        {
            foreach (var state in _states)
            {
                output.Write("{0}->", state.Key);

                bool isFirst = true;
                foreach (var transition in state.Value)
                {
                    if (!isFirst)
                    {
                        output.Write('|');
                    }
                    output.Write("{0}{1}", transition.Out, transition.To);
                    isFirst = false;
                }
                output.WriteLine();
            }
        }

        #endregion

    }

    internal enum ReadType
    {
        Left,
        Right
    }

    internal struct Transition
    {
        public Transition(string to, char @out)
        {
            To = to;
            Out = @out;
        }

        public string To { get; set; }
        public char Out { get; set; }


    }
}
