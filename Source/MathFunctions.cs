using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calculator
{
    public static class MathFunctions
    {
        //要求f为单变量函数
        public delegate double OneVariableFunction(double x);

        //求积分，simpson公式
        const double integral_eps = 1E-13;
        const int start_N = 16;
        
        public static double Integral(OneVariableFunction f, double start_x, double end_x)
        {
            try
            {
                if (start_x >= end_x) return double.NaN;
                int n = start_N;
                double delta;
                double newsum = (end_x - start_x) * (f(start_x) + f(end_x)) / 2;
                double oldsum = 0;
                do
                {
                    oldsum = newsum;
                    newsum = 0;
                    n *= 2;
                    delta = (end_x - start_x) / n;
                    for (int i = 0; i < n; i++)
                    {
                        newsum += (f(start_x + i * delta) + 4 * f(start_x + (i + 0.5) * delta) + f(start_x + (i + 1) * delta)) * delta / 6;
                    }
                } while (Math.Abs(newsum - oldsum) >= integral_eps);
                return newsum;
            }
            catch
            {
                return double.NaN;
            }
        }

        //求导，参考资料https://zhuanlan.zhihu.com/p/109755675
        static readonly double sqrt_u = Math.Pow(2, -52 / 2);
        public static double Derivative(OneVariableFunction f, double x)
        {
            try
            {
                return (f(x + sqrt_u) - f(x - sqrt_u)) / sqrt_u / 2;
            }
            catch
            {
                return double.NaN;
            }
        }

        //求零点，牛顿法
        const double solve_feps = 1E-13;
        const double solve_xeps = 1E-13;
        const double range = 1E5;
        const double begin = 0;
        public static double Solve(OneVariableFunction f,double start_x = -range, double end_x = range)
        {
            try
            {
                double x = begin;
                double fx = f(x);
                if (fx == 0) return x;
                double dfx = Derivative(f, x);
                double oldx = x;
                x -= fx / dfx;
                while (Math.Abs(fx)>=solve_feps||Math.Abs(x-oldx)>=solve_xeps)
                {
                    fx = f(x);
                    dfx = Derivative(f, x);
                    oldx = x;
                    x -= fx / dfx;
                }
                return x;
            }
            catch
            {
                return double.NaN;
            }
        }
    }
}
