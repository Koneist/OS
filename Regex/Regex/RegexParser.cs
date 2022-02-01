using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Regex
{
    internal static class RegexParser
    {

        public static Node Parse(string regex)
        {
            Node firstNode;
            if (regex.Length == 0)
                firstNode = ParseToNode(regex);
            else
            {
                regex = ParseStr(regex);
                firstNode = ParseToNode(regex);
            }

            var noNullNode = CleanEmpty(firstNode);

            return noNullNode;
        }

        public static List<string> ToStringList(Node startNode)
        {
            Stack<Node> nodes = new();
            nodes.Push(startNode);
            List<int> visitedNodes = new() { startNode.Id };
            List<string> transitionsList = new();
            while (nodes.Count > 0)
            {
                var currNode = nodes.Pop();
                StringBuilder sb = new();
                string EndSymb = string.Empty;
                if (currNode.IsEnd)
                    EndSymb = "*";
                sb.AppendFormat("{0}{1}->", currNode.Id, EndSymb);
                bool first = true;
                foreach (var transition in currNode.To)
                {
                    if (first)
                        first = false;
                    else
                        sb.Append('|');

                    if (visitedNodes.IndexOf(transition.Node.Id) == -1)
                    {
                        nodes.Push(transition.Node);
                        visitedNodes.Add(transition.Node.Id);
                    }

                    sb.Append(transition.Out);
                    sb.Append(transition.Node.Id);

                }
                transitionsList.Add(sb.ToString());
            }
            transitionsList.Sort((a,b) => a[0].CompareTo(b[0]));
            return transitionsList;
        }

        private static Node CleanEmpty(Node firstNode)
        {
            var currNode = firstNode;
            var nodes = InitilizeNodesArray(currNode);
            Dictionary<Node, List<NodeTransition>> newTransitions = new();

            foreach(var node in nodes)
            {
                List<Node> nullNodes = GetNullNodes(node);
                newTransitions.Add(node, GetTransitions(nullNodes));
            }

            MinimizeStates(newTransitions);

            foreach (var node in newTransitions)
                node.Key.To = node.Value;

            return currNode;
        }

        private static void MinimizeStates(Dictionary<Node, List<NodeTransition>> newTransitions)
        {
            foreach(var currPair in newTransitions)
            {
                var currNode = currPair.Key;
                var currTransitions = currPair.Value;
                List<Node> replaceableNodes = new();

                newTransitions.Where(x => x.Key != currNode && x.Value.SequenceEqual(currTransitions))
                              .ToList().ForEach(x => replaceableNodes.Add(x.Key));

                if (replaceableNodes.Count == 0)
                    continue;

                foreach(var newPair in newTransitions)
                {
                    var forReplace = newPair.Value.Where(x => replaceableNodes.Contains(x.Node));
                    foreach (var replace in forReplace)
                        replace.Node = currNode;
                }
            }
        }

        private static List<NodeTransition> GetTransitions(List<Node> nodes)
        {
            List<NodeTransition> transitions = new();
            foreach(var node in nodes)
            {
                var currResult = node.To.Where(transition => transition.Out != "");
                transitions = transitions.Concat(currResult).ToList();
            }
            transitions.Sort((a, b) => a.Out.CompareTo(b.Out));
            return transitions;
        }

        private static List<Node> GetNullNodes(Node node)
        {
            List<Node> newNodes = new() { node };
            Stack<Node> nodesStack = new(newNodes);
            while(nodesStack.Count > 0)
            {
                var currNode = nodesStack.Pop();
                var currResult = currNode.To.Where(transition => transition.Out == "");
                foreach(var transition in currResult)
                {
                    if(newNodes.IndexOf(transition.Node) == -1)
                    {
                        nodesStack.Push(transition.Node);
                        newNodes.Add(transition.Node);
                        if (transition.Node.IsEnd)
                            node.IsEnd = true;
                    }
                }
            }

            return newNodes;
        }

        private static Node ParseToNode(string regex)
        {
            Queue<Node> Nodes = new();

            var firstExpression = new NodeTransition(new(false, true), regex);
            var firstNode = new Node(new() { firstExpression }, true, false);

            Nodes.Enqueue(firstNode);

            while (Nodes.Count > 0)
            {
                var currNode = Nodes.Dequeue();
                foreach (var transition in currNode.To)
                {
                    bool wasListModified = false;
                    Expression expression = ReadExpression(transition.Out);

                    switch (expression.Type)
                    {
                        case ExpressionType.Concat:
                            Nodes.Enqueue(currNode);
                            Nodes.Enqueue(ParseConcat(transition, expression));
                            break;
                        case ExpressionType.Or:
                            ParseOr(currNode, transition, expression);
                            Nodes.Enqueue(currNode);
                            wasListModified = true;
                            break;
                        case ExpressionType.Lock:
                            Nodes.Enqueue(ParseLock(transition, expression));
                            break;
                        case ExpressionType.iter:
                            Nodes.Enqueue(ParseIter(transition, expression));
                            Nodes.Enqueue(currNode);
                            break;
                        default: break;
                    }

                    if (wasListModified)
                        break;

                }

            }

            return firstNode;
        }

        private static List<Node> InitilizeNodesArray(Node currNode)
        {
            int id = 0;
            currNode.Id = id++;
            List<Node> nodes = new() { currNode };

            for (int i = 0; i < nodes.Count; ++i)
            {
                foreach (var transition in nodes[i].To)
                {
                    if (transition.Node.Id < 0)
                    {
                        transition.Node.Id = id++;
                        nodes.Add(transition.Node);
                    }
                }
            }

            return nodes;
        }


        private static Node ParseLock(NodeTransition transition, Expression expression)
        {
            var newTransition = new NodeTransition(transition.Node, "");
            var newNode = new Node(new() { newTransition }, false, false);
            var LockTransition = new NodeTransition(newNode, expression.Args[1]);
            newNode.To.Add(LockTransition);
            transition.Node = newNode;
            transition.Out = "";
            return newNode;
        }

        private static Node ParseIter(NodeTransition transition, Expression expression)
        {
            var newTransition = new NodeTransition(transition.Node, "");
            var newNode = new Node(new() { newTransition }, false, false);
            var LockTransition = new NodeTransition(newNode, expression.Args[1]);
            newNode.To.Add(LockTransition);
            transition.Node = newNode;
            transition.Out = expression.Args[1];
            return newNode;
        }

        private static void ParseOr(Node node, NodeTransition transition, Expression expression)
        {
            var firstTransition = new NodeTransition(transition.Node, expression.Args[1]);
            var secondTransition = new NodeTransition(transition.Node, expression.Args[0]);
            node.To.Remove(transition);
            node.To.Add(firstTransition);
            node.To.Add(secondTransition);
        }

        private static Node ParseConcat(NodeTransition transition, Expression expression)
        {
            var newTransition = new NodeTransition(transition.Node, expression.Args[1]);
            var newNode = new Node(new() { newTransition }, false, false);
            transition.Node = newNode;
            transition.Out = expression.Args[0];
            return newNode;
        }


        private static Expression ReadExpression(string input)
        {
            ExpressionType expType = ExpressionType.Literal;
            if (input.Length <= 1)
                return new("", expType, input);

            expType = GetOperator(input);
            
            if(expType == ExpressionType.Lock 
                || expType == ExpressionType.iter)
                return new("", expType, input[0..^1]);

            var args = ReadExpressionArgs(input);

            return new(args.Pop(), expType, args.Pop());
        }

        private static Stack<string> ReadExpressionArgs(string input)
        {
            bool wasFirstArgFound = false;
            int i = input.Length - 2;

            StringBuilder arg = new();
            while (i >= 0 && (!wasFirstArgFound || !IsStopOperator(input, i)))
            {
                arg.Insert(0, input[i]);
                if (!IsOperator(input[i]))
                    wasFirstArgFound = true;
                --i;
            }

            Stack<string> args = new();
            var argStr = arg.ToString();
            
            if (i < 0)
            {
                int firstArgLength = 1;
                if (arg[1] == '*' || arg[1] == '+')
                    firstArgLength = 2;

                args.Push(argStr[firstArgLength..]);
                args.Push(argStr[0..firstArgLength]);
            }
            else
            {
                args.Push(argStr);
                args.Push(input[..(i + 1)]);
            }

            return args;
        }

        private static bool IsStopOperator(string input, int i)
        {
            if (IsBinaryOperator(input[i]))
                return true;
            
            if(i > 0 && (input[i] == '*' || input[i] == '+') && IsBinaryOperator(input[i - 1]))
                return true;

            return false;
        }

        private static ExpressionType GetOperator(string input)
        {
            switch (input[^1]) 
            {
                case '.': return ExpressionType.Concat;
                case '|': return ExpressionType.Or;
                case '*': return ExpressionType.Lock;
                case '+': return ExpressionType.iter;
            }
            throw new ArgumentException("Arg does not contain operator");
        }

        private static bool IsBinaryOperator(char с)
        {
            return ".|".IndexOf(с) != -1;
        }

        private static bool IsOperator(char с)
        {
            return ".+*|()".IndexOf(с) != -1;
        }
        static private byte GetPriority(char s)
        {
            switch (s)
            {
                case '(': return 0;
                case ')': return 1;
                case '|': return 2;
                case '.': return 3;
                default: return 4;
            }
        }
        private static string ParseStr(string input)
        {
            input = AddConcatOperator(input);
            string output = string.Empty; //Строка для хранения выражения
            Stack<char> operStack = new(); //Стек для хранения операторов

            for (int i = 0; i < input.Length; i++) //Для каждого символа в входной строке
            {

                //Если символ - оператор
                if (IsBinaryOperator(input[i])) //Если оператор
                {
                    if (operStack.Count > 0) //Если в стеке есть элементы
                        if (GetPriority(input[i]) <= GetPriority(operStack.Peek())) //И если приоритет нашего оператора меньше или равен приоритету оператора на вершине стека
                            output += operStack.Pop().ToString(); //То добавляем последний оператор из стека в строку с выражением

                    if (operStack.Count > 0 &&
                        input[i] == '|' && input[i] == operStack.Peek())
                        output += operStack.Pop().ToString();
                    operStack.Push(char.Parse(input[i].ToString())); //Если стек пуст, или же приоритет оператора выше - добавляем операторов на вершину стека
                }
                else
                {
                    if (input[i] == '(') //Если символ - открывающая скобка
                        operStack.Push(input[i]); //Записываем её в стек
                    else if (input[i] == ')') //Если символ - закрывающая скобка
                    {
                        //Выписываем все операторы до открывающей скобки в строку
                        char s = operStack.Pop();

                        while (s != '(')
                        {
                            output += s.ToString();
                            s = operStack.Pop();
                        }
                    }
                    else //Если любой другой оператор
                    {
                        output += input[i].ToString();
                    }
                }
            }
            while (operStack.Count > 0)
                output += operStack.Pop();

            return output;
        }

        private static string AddConcatOperator(string input)
        {
            if (input.Length < 1)
                return input;
            StringBuilder output = new(input[0].ToString());
            for (int i = 1; i < input.Length; ++i)
            {
                if ((input[i - 1] != '|' && input[i - 1] != '(') 
                    && (!IsOperator(input[i]) || input[i] == '('))
                    output.Append('.');
                output.Append(input[i]);
            }
            return output.ToString();
        }


    }
    internal class Node
    {
        public int Id = -1;
        public List<NodeTransition> To;
        public bool IsStart;
        public bool IsEnd;

        public Node(List<NodeTransition> to, bool isStart, bool isEnd, int id = -1)
        {
            To = to;
            IsStart = isStart;
            IsEnd = isEnd;
            Id = id;
        }

        public Node(bool isStart, bool isEnd, int id = -1)
        {
            IsStart = isStart;
            IsEnd = isEnd;
            To = new();
        }

    }

    internal class NodeTransition
    {
        public Node Node;
        public string Out;

        public NodeTransition(Node node, string @out)
        {
            Node = node;
            Out = @out;
        }
    }

    internal struct Expression
    {
        public string[] Args;
        public ExpressionType Type;

        public Expression(string arg1, ExpressionType type, string arg2 = "")
        {
            Args = new string[2] { arg1, arg2 };
            Type = type;
        }
    }

    internal enum ExpressionType
    {
        Concat,
        Lock,
        iter,
        Or,
        Literal
    }
}

