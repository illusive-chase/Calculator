using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calculator
{
    //定义函数和变量的出错
    internal class VariableError : Exception
    {
        public VariableError(string message) : base("VariableError" + Environment.NewLine + message) { }

    }

    //一个存储函数及变量的表
    public class VariableTable
    {
        public delegate void ValueChangedEventHandler(double newValue);
        public delegate void FormulaChangedEventHandler(string newDesc);

        private Dictionary<string, ValueChangedEventHandler> handlers = new Dictionary<string, ValueChangedEventHandler>();
        private Dictionary<string, FormulaChangedEventHandler> functionHandlers = new Dictionary<string, FormulaChangedEventHandler>();
        private Dictionary<string, double> tables = new Dictionary<string, double>();
        private Dictionary<string, IExpression> functionTables = new Dictionary<string, IExpression>();
        private Dictionary<string, List<string>> functionDescTables = new Dictionary<string, List<string>>();

        //声明一个新函数,但并未设置具体内容
        public void AddFunction(string name, FormulaChangedEventHandler handler = null)
        {
            if (ContainFunction(name))
                throw new VariableError("函数" + name.Split('|')[0] + "重复声明");
            string[] names = name.Split('|');
            functionTables[names[0]] = null;
            functionDescTables[names[0]] = new List<string>();
            if (handler != null)
                functionHandlers[names[0]] = handler;
        }
        //对已经声明好的函数进行内容的设置
        public void SetFunction(string name, string desc, IExpression expression)
        {
            if (!ContainFunction(name))
                throw new VariableError("函数" + name.Split('|')[0] + "不存在");
            string[] names = name.Split('|');
            functionTables[names[0]] = expression;
            functionDescTables[names[0]].Clear();
            functionDescTables[names[0]].Add(desc);
            functionDescTables[names[0]].AddRange(names);
            if (functionHandlers.ContainsKey(names[0]))
                functionHandlers[names[0]](desc);
        }
        //清楚一个声明好的函数
        public void RemoveFunction(string name)
        {
            if (!ContainFunction(name))
                throw new VariableError("函数" + name.Split('|')[0] + "不存在");
            name = name.Split('|')[0];
            functionTables.Remove(name);
            functionDescTables.Remove(name);
            if (functionHandlers.ContainsKey(name))
                functionHandlers.Remove(name);
        }

        //判别一个函数是否已经被声明
        public bool ContainFunction(string name)
        {
            return functionDescTables.ContainsKey(name.Split('|')[0]);
        }

        //声明一个新变量，暂不设置值
        public void Add(string name, ValueChangedEventHandler handler = null)
        {
            if (Contain(name))
                throw new VariableError("变量" + name + "重复声明");
            tables[name] = 0.0;
            if (handler != null)
                handlers[name] = handler;
        }
        //清楚一个声明好的变量
        public void Remove(string name)
        {
            if (!Contain(name))
                throw new VariableError("变量" + name + "不存在");
            tables.Remove(name);
            if (handlers.ContainsKey(name))
                handlers.Remove(name);
        }
        //判别是否存在该变量
        public bool Contain(string name)
        {
            return tables.ContainsKey(name);
        }
        //对已声明变量的set,get
        public double this[string name]
        {
            get
            {
                if (!Contain(name))
                    throw new VariableError("变量" + name + "不存在");
                return tables[name];
            }
            set
            {
                if (!Contain(name))
                    throw new VariableError("变量" + name + "不存在");
                tables[name] = value;
                if (handlers.ContainsKey(name))
                    handlers[name](value);
            }
        }
        //get已经声明且参数个数正确的函数
        public double this[string name, List<IExpression> args]
        {
            get
            {
                if (!ContainFunction(name))
                    throw new VariableError("函数" + name + "不存在");
                if (args.Count + 2 != functionDescTables[name].Count)
                    throw new VariableError("函数" + name + "参数个数不正确");
                List<double> stack = new List<double>();
                List<bool> existed = new List<bool>();
                for (int i = 2; i < functionDescTables[name].Count; ++i)
                {
                    stack.Add(args[i - 2].GetValue(this));
                }
                for (int i = 2; i < functionDescTables[name].Count; ++i)
                {
                    bool e = Contain(functionDescTables[name][i]);
                    existed.Add(e);
                    if (!e) Add(functionDescTables[name][i]);
                    double s = stack[i - 2];
                    stack[i - 2] = this[functionDescTables[name][i]];
                    this[functionDescTables[name][i]] = s;
                }//在这里对函数的参数进行赋值
                IExpression functionExpression = functionTables[name];
                functionTables[name] = null;
                double result = 0.0;
                try
                {
                    result = functionExpression.GetValue(this);
                }
                catch (NullReferenceException)
                {
                    throw new ValueError("函数" + name + "出现循环或递归定义");
                }
                functionTables[name] = functionExpression;
                for (int i = 2; i < functionDescTables[name].Count; ++i)
                {
                    if (existed[i - 2])
                        this[functionDescTables[name][i]] = stack[i - 2];
                    else
                        Remove(functionDescTables[name][i]);
                }
                return result;
            }
        }

    }
}
