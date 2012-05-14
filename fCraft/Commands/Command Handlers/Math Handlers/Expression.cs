//Copyright (C) <2012>  <Jon Baker, Glenn Mariën and Lao Tszy>

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

//Copyright (C) <2012> Lao Tszy (lao_tszy@yahoo.co.uk)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fCraft
{
    //reverse polish notation expression

    public class Expression : IExpressionElement
    {
        private List<IExpressionElement> _expression = new List<IExpressionElement>();
        private Dictionary<string, Variable> _vars = new Dictionary<string, Variable>(); //vars by name
        private Variable[] _varsArray;  //vars by ordes of appearence in constructor
        public IDictionary<string, Variable> Vars { get { return _vars; } }

        public String AssignedFunctionName { get; internal set; }

        public Expression(IEnumerable<string> vars)
        {
            if (null != vars)
            {
                _varsArray = new Variable[vars.Count()];
                int i = 0;
                foreach (var v in vars)
                {
                    _varsArray[i] = new Variable() { Name = v };
                    _vars.Add(v, _varsArray[i]);
                    ++i;
                }
            }
        }

        public Expression Append(IExpressionElement element)
        {
            _expression.Add(element);
            return this;
        }

        //var value can be set by name
        public void Var(string name, double val)
        {
            _vars[name].Value = val;
        }
        //here var values must be given in the same order as in the ctr
        public double Evaluate(params double[] param)
        {
            Stack<double> stack = new Stack<double>();
            EvaluateInternal(param, stack);
            return stack.Pop();
        }

        private void EvaluateInternal(double[] param, Stack<double> stack)
        {
            if (null != param)
            {
                if (param.Length != _varsArray.Length)
                    throw new ArgumentException("wrong number of params");
                for (int i = 0; i < param.Length; ++i)
                    _varsArray[i].Value = param[i];
            }

            foreach (IExpressionElement e in _expression)
                e.Evaluate(stack);
        }
        public string Print()
        {
            Stack<string> stack = new Stack<string>();
            foreach (IExpressionElement e in _expression)
                e.Print(stack);
            return stack.Pop();
        }

        public void MakeEquality()
        {
            if (!(_expression.Last() is Equal))
                throw new ArgumentException("expression is not an equality");
            _expression[_expression.Count - 1] = new EqualityEqual();
        }
        public bool IsEquality()
        {
            return _expression.Last() is EqualityEqual;
        }
        public bool IsInEquality()
        {
            IExpressionElement e = _expression.Last();
            return (e is Less) || (e is Greater);
        }
        public Tuple<double, double> EvaluateAsEquality(params double[] param)
        {
            Stack<double> stack = new Stack<double>();
            EvaluateInternal(param, stack);
            double compRes = stack.Pop();
            return new Tuple<double, double>(compRes, stack.Pop());
        }

        public void Evaluate(Stack<double> stack)
        {
            foreach (IExpressionElement e in _expression)
                e.Evaluate(stack);
        }

        public void Print(Stack<string> stack)
        {
            stack.Push(AssignedFunctionName);
        }
    }

    //elements, like all consts, operators, and functions
    public interface IExpressionElement
    {
        void Evaluate(Stack<double> stack);
        void Print(Stack<string> stack);
    }

    //all the classes except Variable are stateless and those
    //instances can be reused careless. 
    public class Variable : IExpressionElement
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Value);
        }
        public void Print(Stack<string> stack)
        {
            stack.Push(Name);
        }
    }

    public class E : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Math.E);
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("e");
        }
    }

    public class Pi : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Math.PI);
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("pi");
        }
    }

    public class Sum : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(stack.Pop() + stack.Pop());
        }
        public void Print(Stack<string> stack)
        {
            string second = stack.Pop();
            stack.Push("(" + stack.Pop() + "+" + second + ")");
        }
    }

    public class Mul : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(stack.Pop() * stack.Pop());
        }
        public void Print(Stack<string> stack)
        {
            string second = stack.Pop();
            stack.Push(stack.Pop() + "*" + second);
        }
    }

    public class Sub : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            double sub = stack.Pop();
            stack.Push(stack.Pop() - sub);
        }
        public void Print(Stack<string> stack)
        {
            string second = stack.Pop();
            stack.Push("(" + stack.Pop() + "-" + second + ")");
        }
    }

    public class Div : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            double denom = stack.Pop();
            stack.Push(stack.Pop() / denom);
        }
        public void Print(Stack<string> stack)
        {
            string second = stack.Pop();
            stack.Push(stack.Pop() + "/" + second);
        }
    }

    public class Mod : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            double b = stack.Pop();
            stack.Push(stack.Pop() % b);
        }
        public void Print(Stack<string> stack)
        {
            string second = stack.Pop();
            stack.Push("(" + stack.Pop() + "%" + second + ")");
        }
    }

    public class Pow : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            double pow = stack.Pop();
            stack.Push(Math.Pow(stack.Pop(), pow));
        }
        public void Print(Stack<string> stack)
        {
            string second = stack.Pop();
            stack.Push(stack.Pop() + "^" + second);
        }
    }

    public class Negate : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(-stack.Pop());
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("-(" + stack.Pop() + ")");
        }
    }

    public class Sqrt : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Math.Sqrt(stack.Pop()));
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("sqrt(" + stack.Pop() + ")");
        }
    }

    public class Abs : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Math.Abs(stack.Pop()));
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("abs(" + stack.Pop() + ")");
        }
    }

    public class Sign : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Math.Sign(stack.Pop()));
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("sign(" + stack.Pop() + ")");
        }
    }

    public class Sq : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            double d = stack.Pop();
            stack.Push(d * d);
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("sq(" + stack.Pop() + ")");
        }
    }

    public class Exp : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Math.Exp(stack.Pop()));
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("exp(" + stack.Pop() + ")");
        }
    }

    public class Lg : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Math.Log10(stack.Pop()));
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("lg(" + stack.Pop() + ")");
        }
    }

    public class Ln : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Math.Log(stack.Pop()));
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("ln(" + stack.Pop() + ")");
        }
    }
    public class Log : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            double b = stack.Pop();
            stack.Push(Math.Log(stack.Pop(), b));
        }
        public void Print(Stack<string> stack)
        {
            string second = stack.Pop();
            stack.Push("log(" + stack.Pop() + ", " + second + ")");
        }
    }
    public class Sin : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Math.Sin(stack.Pop()));
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("sin(" + stack.Pop() + ")");
        }
    }
    public class Cos : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Math.Cos(stack.Pop()));
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("cos(" + stack.Pop() + ")");
        }
    }
    public class Tan : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Math.Tan(stack.Pop()));
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("tan(" + stack.Pop() + ")");
        }
    }
    public class Sinh : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Math.Sinh(stack.Pop()));
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("sinh(" + stack.Pop() + ")");
        }
    }
    public class Cosh : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Math.Cosh(stack.Pop()));
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("cosh(" + stack.Pop() + ")");
        }
    }
    public class Tanh : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(Math.Tanh(stack.Pop()));
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("tanh(" + stack.Pop() + ")");
        }
    }
    //comparison ops
    public class Greater : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            double b = stack.Pop();
            stack.Push(stack.Pop() > b ? 1.0 : 0);
        }
        public void Print(Stack<string> stack)
        {
            string second = stack.Pop();
            stack.Push("(" + stack.Pop() + ">" + second + ")");
        }
    }
    public class Less : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            double b = stack.Pop();
            stack.Push(stack.Pop() < b ? 1.0 : 0);
        }
        public void Print(Stack<string> stack)
        {
            string second = stack.Pop();
            stack.Push("(" + stack.Pop() + "<" + second + ")");
        }
    }
    //simple equality for use in-exp 
    public class Equal : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            double b = stack.Pop();
            stack.Push(stack.Pop() == b ? 1.0 : 0);
        }
        public void Print(Stack<string> stack)
        {
            string second = stack.Pop();
            stack.Push("(" + stack.Pop() + "=" + second + ")");
        }
    }
    //special equality operation needed for equations, where we need to know whether the equality happened in between of two evaluation points 
    //and also choose between those points 
    public class EqualityEqual : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            double b = stack.Pop();
            double a = stack.Pop();
            stack.Push(Math.Abs(a - b));
            stack.Push(Math.Sign(a - b));
        }
        public void Print(Stack<string> stack)
        {
            string second = stack.Pop();
            stack.Push(stack.Pop() + "=" + second);
        }
    }
    //logical ops
    public class And : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            double b = stack.Pop();
            stack.Push(stack.Pop() != 0 && b != 0 ? 1.0 : 0);
        }
        public void Print(Stack<string> stack)
        {
            string second = stack.Pop();
            stack.Push("(" + stack.Pop() + "&" + second + ")");
        }
    }
    public class Or : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            double b = stack.Pop();
            stack.Push(stack.Pop() != 0 || b != 0 ? 1.0 : 0);
        }
        public void Print(Stack<string> stack)
        {
            string second = stack.Pop();
            stack.Push("(" + stack.Pop() + "|" + second + ")");
        }
    }
    public class Not : IExpressionElement
    {
        public void Evaluate(Stack<double> stack)
        {
            stack.Push(stack.Pop() == 0 ? 1.0 : 0);
        }
        public void Print(Stack<string> stack)
        {
            stack.Push("(!" + stack.Pop() + ")");
        }
    }
}
