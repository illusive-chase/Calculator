using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calculator
{
    //操作集合
    public enum Operator
    {
        ADD,
        SUB,
        MUL,
        DIV,
        POW,
    }

    //报错函数
    internal class ValueError : Exception
    {
        public ValueError(string message) : base("ValueError" + Environment.NewLine + message) {}
    }
    //定义IExpression接口，必须存在判断是否连续，获取对应数值和存在IExpression列表
    public interface IExpression
    {
        bool IsConstant();
        double GetValue(VariableTable vt);
        List<IExpression> GetArguments();
    }

    //定义数字类型
    public class Number : IExpression
    {
        private double value;

        public Number(string value, bool neg)
        {
            this.value = double.Parse(value);
            this.value = neg ? -this.value : this.value;
        }
        public bool IsConstant()
        {
            return true;
        }

        public double GetValue(VariableTable vt)
        {
            return value;
        }

        public List<IExpression> GetArguments()
        {
            return new List<IExpression>();
        }
    }

    //定义变量类型
    public class Variable : IExpression
    {
        private string varName;
        private bool negative;

        public Variable(string varName, bool neg)
        {
            this.varName = varName;
            this.negative = neg;
        }
        public bool IsConstant()
        {
            return false;
        }

        public double GetValue(VariableTable vt)
        {
            return negative ? -vt[varName] : vt[varName];
        }

        public List<IExpression> GetArguments()
        {
            return new List<IExpression>();
        }
    }
    

    //定义函数类型
    public class Function : IExpression
    {
        private string fName;
        private bool negative;
        private List<IExpression> args = new List<IExpression>();
        public Function(string fName, bool neg, params IExpression[] args)
        {
            this.args.AddRange(args);
            this.fName = fName;
            this.negative = neg;
        }

        public bool IsConstant()
        {
            return false;
        }

        public double GetValue(VariableTable vt)
        {
            return negative ? -vt[fName, args] : vt[fName, args];
        }

        public List<IExpression> GetArguments()
        {
            return args;
        }
    }
    //定义积分
    public class IntegralFunction : IExpression
    {
        private string dx;
        private bool negative;
        private List<IExpression> args = new List<IExpression>();
        public IntegralFunction(bool neg, string ub, string lb, IExpression expression, string dx)
        {
            if (char.IsDigit(ub[0]))
                args.Add(new Number(ub, false));
            else
                args.Add(new Variable(ub, false));
            if (char.IsDigit(lb[0]))
                args.Add(new Number(lb, false));
            else
                args.Add(new Variable(lb, false));
            args.Add(expression);
            this.dx = dx;
            this.negative = neg;
        }

        public bool IsConstant()
        {
            return false;
        }

        public double GetValue(VariableTable vt)
        {

            double end_x = args[0].GetValue(vt);
            double start_x = args[1].GetValue(vt);
            bool neg = negative;
            if (start_x > end_x)
            {
                double temp = start_x;
                start_x = end_x;
                end_x = temp;
                neg = !neg;
            }
            double stack = 0;
            bool exist = false;
            if (vt.Contain(dx))
            {
                stack = vt[dx];
                exist = true;
            }
            else
                vt.Add(dx);

            double result = MathFunctions.Integral(
                x => { vt[dx] = x; return args[2].GetValue(vt); },
                start_x,
                end_x
                );


            if (exist) vt[dx] = stack;
            else vt.Remove(dx);

            return negative ? -result : result;
        }

        public List<IExpression> GetArguments()
        {
            return args;
        }
    }

    //自定义函数类型，支持输入参数数为1，2，3的情形
    public class InternalFunction : IExpression
    {
        public delegate double InternalHandler(VariableTable vt);
        public delegate double InternalHandler1(double arg0);
        public delegate double InternalHandler2(double arg0, double arg1);
        public delegate double InternalHandler3(double arg0, double arg1, double arg2);
        private InternalHandler inner;
        private List<IExpression> args = new List<IExpression>();
        public InternalFunction(InternalHandler1 inner)
        {
            args.Add(new Variable("a", false));
            this.inner = vt => inner(args[0].GetValue(vt));
        }

        public InternalFunction(InternalHandler2 inner)
        {
            args.Add(new Variable("a", false));
            args.Add(new Variable("b", false));
            this.inner = vt => inner(args[0].GetValue(vt), args[1].GetValue(vt));
        }

        public InternalFunction(InternalHandler3 inner)
        {
            args.Add(new Variable("a", false));
            args.Add(new Variable("b", false));
            args.Add(new Variable("c", false));
            this.inner = vt => inner(args[0].GetValue(vt), args[1].GetValue(vt), args[2].GetValue(vt));
        }

        public bool IsConstant()
        {
            return false;
        }

        public double GetValue(VariableTable vt)
        {
            return inner(vt);
        }

        public List<IExpression> GetArguments()
        {
            return args;
        }
    }

    //定义基本运算
    public class BinaryOp : IExpression
    {
        private IExpression lhs, rhs;
        private Operator op;

        private List<IExpression> arguments = new List<IExpression>();

        public BinaryOp(IExpression lhs, IExpression rhs, Operator op)
        {
            this.lhs = lhs;
            this.rhs = rhs;
            arguments.Add(lhs);
            arguments.Add(rhs);
            this.op = op;
        }

        public List<IExpression> GetArguments()
        {
            return arguments;
        }

        public bool IsConstant()
        {
            return lhs.IsConstant() && rhs.IsConstant();
        }

        public double GetValue(VariableTable vt)
        {
            double lvalue = lhs.GetValue(vt);
            double rvalue = rhs.GetValue(vt);
            double value = 0;
            switch (op)
            {
                case Operator.ADD:
                    value = lvalue + rvalue;
                    break;
                case Operator.SUB:
                    value = lvalue - rvalue;
                    break;
                case Operator.MUL:
                    value = lvalue * rvalue;
                    break;
                case Operator.DIV:
                    if (rvalue == 0.0)
                        throw new ValueError("除数为0");
                    value = lvalue / rvalue;
                    break;
                case Operator.POW:
                    if (rvalue == 0.0 && lvalue == 0.0)
                        throw new ValueError("试图计算0的0次方");
                    if (rvalue < 0.0 && Math.Round(rvalue) != rvalue)
                        throw new ValueError("幂指数非整数却为负");
                    value = Math.Pow(lvalue, rvalue);
                    break;
                default:
                    break;
            }
            return value;
        }


    }

}
