using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Calculator
{
    //定义句法错误
    internal class SyntaxError : Exception
    {
        public SyntaxError(string message) : base("SyntaxError" + Environment.NewLine + message) { }

        public static void Throw(string raw, int pos, string message)
        {
            throw new SyntaxError(
                raw 
                + Environment.NewLine + new string(' ', pos) + "^" 
                + Environment.NewLine + message
                );
        }
    }

    
    //在这个类中完成对输入的解析
    public class Parser
    {

        private delegate IExpression EmbedLambda(IExpression expression);

        private enum Token
        {
            Add,
            Sub,
            Pow,
            Mul,
            Div,
            LB,
            RB,
            EQ,
            Comma,
            Space,
            Int,
            Number,
            Variable,
            LCB,
            RCB,
        }

        static string[] re =
        {
            @"^\+",
            @"^\-",
            @"^\^",
            @"^\\times|^\*",
            @"^\/",
            @"^\(",
            @"^\)",
            @"^\=",
            @"^\,",
            @"^[\t\r\n ]+",
            @"^\\int_[A-z0-9]\^[A-z0-9]|^\\int\^[A-z0-9]_[A-z0-9]",
            @"^0\.\d*|^0|^\.\d+|^[1-9]\d*\.\d*|^[1-9]\d*",
            @"^(\\[A-z]+|[A-z]\'*(_[A-z0-9]|_\{[A-z0-9]+\})?\'*)",
            @"^\{",
            @"^\}",
        };

        //判断从index位置开始(包括index)的子串是否为对应的token
        private static string TryToken(string s, Token token, int index)
        {
            Match match = Regex.Match(s.Substring(index), re[(int)token]);
            if (match.Success) return match.Groups[0].Value;
            return null;
        }

        //下面是涉及到语句的组合逻辑的一些设计
        private static IExpression N(string raw, List<KeyValuePair<Token, string>> tokens, List<int> positions, ref int index)
        {
            // N->(E) | ITEM | +ITEM | -ITEM
            IExpression result = null;
            if (tokens.Count <= index)
                SyntaxError.Throw(raw, positions[index], "缺少表达式");
            if (tokens[index].Key == Token.LB)
            {
                index++;
                result = E(raw, tokens, positions, ref index);
                if (tokens.Count <= index || tokens[index].Key != Token.RB)
                    SyntaxError.Throw(raw, positions[index], "缺少右括号");
                index++;
                return result;
            }
            bool neg = false;
            if (tokens[index].Key == Token.Sub || tokens[index].Key == Token.Add)
            {
                neg = tokens[index].Key == Token.Sub;
                index++;
            }
            if (tokens[index].Key != Token.Int && tokens[index].Key != Token.Variable && tokens[index].Key != Token.Number)
                SyntaxError.Throw(raw, positions[index], "此处应为常数或变量");
            if (tokens[index].Key == Token.Int)
            {
                string ub, lb;
                Match match = Regex.Match(tokens[index].Value, @"^\\int_([A-z0-9])\^([A-z0-9])$|^\\int\^([A-z0-9])_([A-z0-9])$");
                if (!match.Success ||
                    ((!match.Groups[1].Success || !match.Groups[2].Success) &&
                    (!match.Groups[3].Success || !match.Groups[4].Success))
                    )
                    throw new Exception("未知的错误：发生在解析积分" + tokens[index].Key + "时");

                if (!match.Groups[1].Success || !match.Groups[2].Success)
                {
                    ub = match.Groups[3].Value;
                    lb = match.Groups[4].Value;
                }
                else
                {
                    ub = match.Groups[2].Value;
                    lb = match.Groups[1].Value;
                }

                index++;
                if (tokens.Count <= index || tokens[index].Key != Token.LCB)
                    SyntaxError.Throw(raw, positions[index], "积分运算参数不能少于2个");
                index++;
                IExpression exp1 = E(raw, tokens, positions, ref index);
                if (tokens.Count <= index || tokens[index].Key != Token.RCB)
                    SyntaxError.Throw(raw, positions[index], "缺少右花括号");
                index++;
                if (tokens.Count <= index || tokens[index].Key != Token.LCB)
                    SyntaxError.Throw(raw, positions[index], "积分运算参数不能少于2个");
                index++;
                if (tokens.Count <= index || tokens[index].Key != Token.Variable || tokens[index].Value != "d")
                    SyntaxError.Throw(raw, positions[index], "积分运算第2个参数必须是某个变量的微分符");
                index++;
                if (tokens.Count <= index || tokens[index].Key != Token.Variable)
                    SyntaxError.Throw(raw, positions[index], "积分运算第2个参数必须是某个变量的微分符");
                string dx = tokens[index].Value;
                index++;
                if (tokens.Count <= index || tokens[index].Key != Token.RCB)
                    SyntaxError.Throw(raw, positions[index], "缺少右花括号");
                index++;
                result = new IntegralFunction(neg, ub, lb, exp1, dx);   
            }
            else if (tokens[index].Key == Token.Variable)
            {
                string name = tokens[index++].Value;
                if (tokens.Count > index && tokens[index].Key == Token.LCB)
                {
                    index++;
                    IExpression exp1 = E(raw, tokens, positions, ref index);
                    if (tokens.Count <= index || tokens[index].Key != Token.RCB)
                        SyntaxError.Throw(raw, positions[index], "缺少右花括号");
                    index++;
                    if (tokens.Count > index && tokens[index].Key == Token.LCB)
                    {
                        index++;
                        IExpression exp2 = E(raw, tokens, positions, ref index);
                        if (tokens.Count <= index || tokens[index].Key != Token.RCB)
                            SyntaxError.Throw(raw, positions[index], "缺少右花括号");
                        index++;
                        result = new Function(name, neg, exp1, exp2);
                        return result;
                    }
                    result = new Function(name, neg, exp1);
                    return result;
                }
                if (tokens.Count > index && tokens[index].Key == Token.LB)
                {
                    index++;
                    List<IExpression> expressions = new List<IExpression>();
                    expressions.Add(E(raw, tokens, positions, ref index));
                    while (tokens.Count > index && tokens[index].Key == Token.Comma)
                    {
                        index++;
                        expressions.Add(E(raw, tokens, positions, ref index));
                    }
                    if (tokens.Count <= index || tokens[index].Key != Token.RB)
                        SyntaxError.Throw(raw, positions[index], "缺少右括号");
                    index++;
                    result = new Function(name, neg, expressions.ToArray());
                    return result;
                }
                result = new Variable(name, neg);
            }
            else
                result = new Number(tokens[index++].Value, neg);
            return result;
        }

        private static IExpression F_(string raw, List<KeyValuePair<Token, string>> tokens, List<int> positions, ref int index)
        {
            // F'->^F|ε
            IExpression result = null;
            if (tokens.Count <= index) return result;
            if (tokens[index].Key == Token.Pow)
            {
                index++;
                result = F(raw, tokens, positions, ref index);
                return result;
            }
            if (tokens[index].Key == Token.LB || tokens[index].Key == Token.Number || tokens[index].Key == Token.Variable)
                SyntaxError.Throw(raw, positions[index], "缺少运算符");
            return result;
        }

        private static IExpression F(string raw, List<KeyValuePair<Token, string>> tokens, List<int> positions, ref int index)
        {
            // F->NF'
            IExpression result = N(raw, tokens, positions, ref index);
            IExpression ex = F_(raw, tokens, positions, ref index);
            if (ex != null)
            {
                result = new BinaryOp(result, ex, Operator.POW);
            }
            return result;
        }

        private static EmbedLambda T_(string raw, List<KeyValuePair<Token, string>> tokens, List<int> positions, ref int index)
        {
            // T'->*FT'|/FT'|ε
            IExpression result = null;
            if (tokens.Count <= index) return (x) => x;
            if (tokens[index].Key == Token.Mul || tokens[index].Key == Token.Div)
            {
                Operator op = tokens[index].Key == Token.Mul ? Operator.MUL : Operator.DIV;
                index++;
                result = F(raw, tokens, positions, ref index);
                EmbedLambda rhs = T_(raw, tokens, positions, ref index);
                return (x) => rhs(new BinaryOp(x, result, op));
            }
            if (tokens[index].Key == Token.LB || tokens[index].Key == Token.Pow || tokens[index].Key == Token.Number || tokens[index].Key == Token.Variable)
                SyntaxError.Throw(raw, positions[index], "缺少运算符");
            return (x) => x;
        }

        private static IExpression T(string raw, List<KeyValuePair<Token, string>> tokens, List<int> positions, ref int index)
        {
            // T ->FT'
            IExpression result = F(raw, tokens, positions, ref index);
            EmbedLambda rhs = T_(raw, tokens, positions, ref index);
            return rhs(result);
        }

        private static EmbedLambda E_(string raw, List<KeyValuePair<Token, string>> tokens, List<int> positions, ref int index)
        {
            // E'->+TE' | -TE'|ε
            IExpression result = null;
            if (tokens.Count <= index) return (x) => x;
            if (tokens[index].Key == Token.Add || tokens[index].Key == Token.Sub)
            {
                Operator op = tokens[index].Key == Token.Add ? Operator.ADD : Operator.SUB;
                index++;
                result = T(raw, tokens, positions, ref index);
                EmbedLambda rhs = E_(raw, tokens, positions, ref index);
                return (x) => rhs(new BinaryOp(x, result, op));
            }
            if (tokens[index].Key != Token.RB && tokens[index].Key != Token.RCB && tokens[index].Key != Token.Comma)
                SyntaxError.Throw(raw, positions[index], "缺少运算符");
            return (x) => x;
        }

        private static IExpression E(string raw, List<KeyValuePair<Token, string>> tokens, List<int> positions, ref int index)
        {
            // E ->TE'
            
            IExpression result = T(raw, tokens, positions, ref index);
            EmbedLambda rhs = E_(raw, tokens, positions, ref index);
            return rhs(result);
        }

        private static KeyValuePair<string, IExpression> S(string raw, List<KeyValuePair<Token, string>> tokens, List<int> positions, ref int index)
        {
            // S ->V(V,...,)=E|V=E|E

            string varName = null;
            IExpression expression = null;
            if (raw.Substring(index).Contains("="))
            {
                if (index + 1 < tokens.Count && tokens[index].Key == Token.Variable && tokens[index + 1].Key == Token.EQ)
                {
                    varName = tokens[index].Value;
                    index += 2;
                }
                else if (index + 1 < tokens.Count && tokens[index].Key == Token.Variable && tokens[index + 1].Key == Token.LB)
                {
                    varName = tokens[index].Value;
                    index += 2;
                    while (index < tokens.Count && tokens[index].Key == Token.Variable)
                    {
                        varName += '|' + tokens[index].Value;
                        index++;
                        if (index >= tokens.Count || (tokens[index].Key != Token.Comma) && (tokens[index].Key != Token.RB))
                            SyntaxError.Throw(raw, positions[index], "缺少右括号或逗号");
                        if (tokens[index].Key == Token.Comma)
                            index++;
                    }
                    if (index >= tokens.Count || tokens[index].Key != Token.RB)
                        SyntaxError.Throw(raw, positions[index], "缺少右括号");
                    index++;
                    if (index >= tokens.Count || tokens[index].Key != Token.EQ)
                        SyntaxError.Throw(raw, positions[index], "缺少等号");
                    index++;
                }
            }
            expression = E(raw, tokens, positions, ref index);
            return new KeyValuePair<string, IExpression>(varName, expression);
        }

        public static KeyValuePair<string, IExpression> Parse(string s)
        {

            /*
            S->E|v=E
            E->E+T|E-T|T
            T->T*F|T/F|F
            F->N^F|N
            N->(E)|I|+I|-I
            I->v{E}{E}|v{E}|v(E,E)|v(E)|v()|v|d

                                       First           Follow
            S ->v=E|E                [(,+,-,v,d]     [#]
            E ->TE'                  [(,+,-,v,d]     [),},#]
            E'->+TE'|-TE'|ε          [+,-,ε]         [),},#]
            T ->FT'                  [(,+,-,v,d]     [+,-,),},#]
            T'->*FT'|/FT'|ε          [/,*,ε]         [+,-,),},#]
            F ->NF'                  [(,+,-,v,d]     [+,-,),},#,*,/]
            F'->^F|ε                 [^,ε]           [+,-,),},#,*,/]
            N->(E)|I|+I|-I           [(,+,-,v,d]     [+,-,),},#,*,/,^]
            I->v{E}{E}|v{E}|v|d      [v,d]           [+,-,),},#,*,/,^]

             */

            List<KeyValuePair<Token, string>> tokens = new List<KeyValuePair<Token, string>>();
            List<int> positions = new List<int>();

            int pos = 0;
            while (pos < s.Length)//依照循环从起始位置向终止位置切句子并判断token
            {
                string match = null;
                Token token = Token.Space;
                for (int i = 0; i < re.Length; ++i)
                {
                    token = (Token)i;
                    match = TryToken(s, token, pos);
                    if (match != null) break;
                }
                if (match == null)
                {
                    SyntaxError.Throw(s, pos, "此位置出现无法解析的词素");
                }
                if (token != Token.Space)
                {
                    tokens.Add(new KeyValuePair<Token, string>(token, match));
                    positions.Add(pos);
                }
                pos += match.Length;
            }
            positions.Add(pos);

            int index = 0;
            //将得到的tokens与positions信息带入S作为S的内容，其后进入之前写好的句式逻辑判断中，尝试得到一个合理的解释
            KeyValuePair<string, IExpression> assignment = S(s, tokens, positions, ref index);
            //若解释出现冗余词素则报错
            if (index < tokens.Count) SyntaxError.Throw(s, positions[index], "此位置出现冗余的词素");
            return assignment;
        }
    }
}
