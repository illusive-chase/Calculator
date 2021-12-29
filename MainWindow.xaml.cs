using Calculator;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;

namespace UI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        private VariableTable vt = new VariableTable();//记录变量及函数的表格
        private int historyCount = 0;//记录当前历史信息的条数

        public MainWindow()
        {
            InitializeComponent();//初始化控件
            RegisterDefaultSymbols();
            ssvFormulaHistoryViewer.AddHandler(MouseWheelEvent, new MouseWheelEventHandler(WheelAnyway), true);
            ssvVariablesViewer.AddHandler(MouseWheelEvent, new MouseWheelEventHandler(WheelAnyway), true);
            ssvFunctionsViewer.AddHandler(MouseWheelEvent, new MouseWheelEventHandler(WheelAnyway), true);

            KeyboardFocusChangedEventHandler dehint = (sender, args) =>
            {
                if (txtInputBox.Text == "Input formula here")
                {
                    txtInputBox.Text = "";
                    txtInputBox.Foreground = Brushes.Black;
                }
            };
            KeyboardFocusChangedEventHandler hint = (sender, args) =>
            {
                if (txtInputBox.Text.Length == 0)
                {
                    txtInputBox.IsEnabled = false;
                    txtInputBox.Text = "Input formula here";
                    txtInputBox.Foreground = Brushes.LightGray;
                    txtInputBox.IsEnabled = true;
                }
            };
            hint(null, null);//初始化输入栏
            txtInputBox.LostKeyboardFocus += hint;//鼠标选中输入栏和离开输入栏时启用事件
            txtInputBox.GotKeyboardFocus += dehint;

        }

        //注册基本的运算和函数(声明并赋予具体内容)
        private void RegisterDefaultSymbols()
        {
            vt.Add(@"\pi");
            vt[@"\pi"] = Math.PI;
            vt.Add(@"e");
            vt[@"e"] = Math.E;
            vt.AddFunction(@"\sqrt");
            vt.SetFunction(@"\sqrt|a", "", new InternalFunction(Math.Sqrt));
            vt.AddFunction(@"\frac");
            vt.SetFunction(@"\frac|a|b", "", new InternalFunction((a, b) => a / b));
            vt.AddFunction(@"\exp");
            vt.SetFunction(@"\exp|a", "", new InternalFunction(Math.Exp));
            vt.AddFunction(@"\log");
            vt.SetFunction(@"\log|a", "", new InternalFunction(a => Math.Log(a)));
            vt.AddFunction(@"\ln");
            vt.SetFunction(@"\ln|a", "", new InternalFunction(a => Math.Log(a)));
            vt.AddFunction(@"\sin");
            vt.SetFunction(@"\sin|a", "", new InternalFunction(Math.Sin));
            vt.AddFunction(@"\cos");
            vt.SetFunction(@"\cos|a", "", new InternalFunction(Math.Cos));
            vt.AddFunction(@"\tan");
            vt.SetFunction(@"\tan|a", "", new InternalFunction(Math.Tan));
            vt.AddFunction(@"\asin");
            vt.SetFunction(@"\asin|a", "", new InternalFunction(Math.Asin));
            vt.AddFunction(@"\acos");
            vt.SetFunction(@"\acos|a", "", new InternalFunction(Math.Acos));
            vt.AddFunction(@"\atan");
            vt.SetFunction(@"\atan|a", "", new InternalFunction(Math.Atan));
            vt.AddFunction(@"\max");
            vt.SetFunction(@"\max|a|b", "", new InternalFunction(Math.Max));
            vt.AddFunction(@"\min");
            vt.SetFunction(@"\min|a|b", "", new InternalFunction(Math.Min));

        }

        //更改当前出错的内容，记录在statusText.Text中供调用
        private void UpdateStatusText(string info)
        {
            tbStatusText.Text = info;
        }


        //进行滑动操作时的动画设置
        private void WheelAnyway(object sender, MouseWheelEventArgs args)
        {
            //var presenter = VisualTreeHelper.GetParent(sender as DependencyObject) as ScrollContentPresenter;
            var scroller = sender as SmoothScrollViewer;
            DoubleAnimation animation = new DoubleAnimation
            {
                From = scroller.VerticalOffset,
                To = scroller.VerticalOffset - args.Delta * 2,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase()
                {
                    EasingMode = EasingMode.EaseOut
                }
            };
            Timeline.SetDesiredFrameRate(animation, 30);
            scroller.BeginAnimation(SmoothScrollViewer.VerticalScrollAmountProperty, animation);
        }

        //输入栏改变时的操作
        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //输入栏不可用时将输入栏和渲染内容置为固定内容
            if (!txtInputBox.IsEnabled)
            {
                fcFormulaControl.Formula = @"Formula\;to\;be\;rendered\;here";
                btnCalculate.IsEnabled = false;
                return;
            }
            //可用情况下尝试得到渲染结果，并更新calculate按钮
            try
            {
                fcFormulaControl.Formula = txtInputBox.Text;
                if (fcFormulaControl.HasError)
                    throw new Exception();
                btnCalculate.IsEnabled = txtInputBox.Text.Length > 0;
                UpdateStatusText("");
            }
            catch (Exception)
            {
                UpdateStatusText("无法识别公式");
                btnCalculate.IsEnabled = false;
            }
        }
        //calculate按钮的click事件，调用calculate进行计算
        private void ButtonCalculate_Click(object sender, RoutedEventArgs e)
        {
            CalculateInput(false);
        }


        //计算函数
        private void CalculateInput(bool toSetCursor)
        {
            //如果按钮不可用，说明公式有误，此时阻止键盘输入引起的调用
            if (!btnCalculate.IsEnabled) return;
            try
            {
                KeyValuePair<string, IExpression> assignment = Parser.Parse(txtInputBox.Text);//调用calculator中的Parser.Parse函数进行解析
                IExpression expression = assignment.Value;//返回解析结果
                if (assignment.Key != null && assignment.Key.Contains("|"))
                {//若解析发现不存在函数则定义一个函数，并调用GenerateHistory记录在历史中
                    if (!vt.ContainFunction(assignment.Key))
                        AddFunction(assignment.Key);
                    vt.SetFunction(assignment.Key, txtInputBox.Text, expression);
                    UpdateStatusText("函数定义成功");
                    GenerateHistory(txtInputBox.Text);
                }
                else
                {
                    double result = expression.GetValue(vt);
                    string historyFormulaString = result.ToString() + @"\;\;\;\;\;\;\;";
                    if (assignment.Key == null)
                    {
                        UpdateStatusText(string.Format("求值成功: {0}", result));
                        if (!(expression is Number))
                            historyFormulaString = txtInputBox.Text + "=" + historyFormulaString;
                    }
                    else
                    {
                        if (!vt.Contain(assignment.Key))
                            AddVariable(assignment.Key);
                        vt[assignment.Key] = result;
                        UpdateStatusText(string.Format("赋值成功: {0}={1}", assignment.Key, result));
                        historyFormulaString = assignment.Key + "=" + historyFormulaString;
                    }
                    GenerateHistory(historyFormulaString);
                    txtInputBox.Text = result.ToString();
                    if (toSetCursor)
                        txtInputBox.Select(txtInputBox.Text.Length, 0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "错误");
                UpdateStatusText("失败");
            }
        }
        //产生历史记录的函数
        private void GenerateHistory(string historyFormulaString)
        {
            //生成历史记录信息并设置一些显示的数据
            historyCount += 1;
            historyFormulaString = string.Format(@"[{0}]:\;", historyCount) + historyFormulaString;
            WpfMath.Controls.FormulaControl formula = new WpfMath.Controls.FormulaControl();
            formula.Formula = historyFormulaString;
            formula.Height = 100;
            formula.Margin = new Thickness(15, 0, 15, 0);
            formula.VerticalContentAlignment = VerticalAlignment.Center;

            //增加关闭按钮并增加相应的事件
            spFormulaHistory.Children.Add(AddStackPanelItemWithCloseButton(formula, null, (sender, e) =>
            {
                txtInputBox.Focus();
                txtInputBox.Text = Regex.Replace(formula.Formula, @"\[.*?\]:\\;(.*?)(=.*|\\;.*)", "$1");
                txtInputBox.Select(txtInputBox.Text.Length, 0);
            }));

            //增加滑动时的动画设置
            WheelAnyway(ssvFormulaHistoryViewer, new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, (int)(ssvFormulaHistoryViewer.VerticalOffset - ssvFormulaHistoryViewer.ScrollableHeight - formula.Height * 2)));
        }

        //通过输入框的KeyDown事件也能触发计算
        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                CalculateInput(true);
            }
        }

        //删除按钮，将渲染出的结果和历史清空
        private void ButtonClear_Click(object sender, RoutedEventArgs e)
        {
            spFormulaHistory.Children.Clear();
            historyCount = 0;
        }

        //增加变量的按钮，依照输入框增加变量
        private void ButtonAddVariable_Click(object sender, RoutedEventArgs e)
        {
            string s = txtInputBox.Text;
            Match match = Regex.Match(s, @"^\\[A-z]+$|^([A-z]\'*(_[A-z0-9]|_\{[A-z0-9]+\})?\'*)$");
            if (!match.Success)
            {
                UpdateStatusText("无效的变量名");
                return;
            }
            if (vt.Contain(s))
            {
                UpdateStatusText("重复的变量名");
                return;
            }
            AddVariable(s);
        }


        private UIElement AddStackPanelItemWithCloseButton(FrameworkElement item, RoutedEventHandler handler = null, MouseButtonEventHandler doubleClick = null)
        {   //建立一个删除按钮
            Button button = new Button
            {
                Margin = new Thickness(0, 0, 15, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Content = "⨉",
                Foreground = Brushes.White,
                Background = Brushes.Red,
                Visibility = Visibility.Hidden,
                Width = 20,
                Height = 20
            };
            //生成一个网格结构
            Grid grid = new Grid();
            grid.Children.Add(item);
            grid.Children.Add(button);

            ScrollViewer viewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = grid,
                Style = (Style)FindResource("horizontal")
            };
            //删除按键的可视化改变
            viewer.MouseEnter += (sender, e) => { button.Visibility = Visibility.Visible; };
            viewer.MouseLeave += (sender, e) => { button.Visibility = Visibility.Hidden; };

            if (doubleClick != null) viewer.MouseDoubleClick += doubleClick;

            //可视化上的处理，绘制边框等
            Border border = new Border
            {
                Child = viewer,
                Height = item.Height,
                Background = Brushes.White,
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(15, 15, 15, 15),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 5,
                    Color = Color.FromRgb(0xF0, 0xF0, 0xF0),
                    ShadowDepth = 0
                },
            };

            //设置删除按键的Click事件
            if (handler == null)
                button.Click += (sender, e) =>
                {
                    (border.Parent as StackPanel).Children.Remove(border);
                };
            else
                button.Click += (sender, e) =>
                {
                    handler(sender, e);
                    (border.Parent as StackPanel).Children.Remove(border);
                };
            //设置相应的动画处理
            DoubleAnimation animation = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400)
            };

            border.BeginAnimation(Border.OpacityProperty, animation);
            return border;
        }

        //具体的增加变量的函数
        private void AddVariable(string v)
        {
            //新建一个在变量栏的变量显示
            WpfMath.Controls.FormulaControl formula = new WpfMath.Controls.FormulaControl
            {
                Formula = v + "=0",
                Height = 70,
                Margin = new Thickness(15, 0, 15, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            //调用Calculator中的Variable.Add增加变量
            vt.Add(v, (newValue) => { formula.Formula = v + "=" + newValue.ToString(); });
            //为之前建立的变量显示增加关闭按钮并设置事件
            spVariables.Children.Add(AddStackPanelItemWithCloseButton(
                formula,
                (sender, e) => { vt.Remove(v); },
                (sender, e) => { txtInputBox.Focus(); txtInputBox.Text = v; txtInputBox.Select(txtInputBox.Text.Length, 0); }
            ));
        }
        //具体的增加函数的函数
        private void AddFunction(string v)
        {
            //新建一个在函数栏的函数显示
            WpfMath.Controls.FormulaControl formula = new WpfMath.Controls.FormulaControl
            {
                Formula = "",
                Height = 70,
                Margin = new Thickness(15, 0, 15, 0),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            //调用Calculator中的Variable.AddFunction增加函数
            if (!vt.ContainFunction(v))
                vt.AddFunction(v, (newDesc) => { formula.Formula = newDesc; });
            //为之前建立的函数显示增加关闭按钮并设置事件
            spFunctions.Children.Add(AddStackPanelItemWithCloseButton(
                formula,
                (sender, e) => { vt.RemoveFunction(v); },
                (sender, e) =>
                {
                    int index = v.IndexOf('|');
                    txtInputBox.Focus();
                    txtInputBox.Text = v.Substring(0, index) + '(' + v.Substring(index + 1).Replace('|', ',') + ')';
                    txtInputBox.Select(txtInputBox.Text.Length, 0);
                }
            ));
        }
        //保存按钮，保存公式或图片
        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = "PNG Files (*.png)|*.png|TXT Files (*.txt)|*.txt"
            };
            var result = saveFileDialog.ShowDialog();
            if (result == false) return;
            switch (saveFileDialog.FilterIndex)
            {
                case 1:
                    FileStream stream = new FileStream(saveFileDialog.FileName, FileMode.Create);
                    WpfMath.TexFormulaParser parser = new WpfMath.TexFormulaParser();
                    WpfMath.TexFormula formula = parser.Parse(txtInputBox.Text);
                    BitmapSource bitmap = formula.GetRenderer(WpfMath.TexStyle.Display, fcFormulaControl.Scale, "Arial").RenderToBitmap(0, 0, 300);
                    PngBitmapEncoder encoder = new PngBitmapEncoder
                    {
                        Frames = { BitmapFrame.Create(bitmap) }
                    };
                    encoder.Save(stream);
                    stream.Close();
                    break;
                case 2:
                    StreamWriter writer = new StreamWriter(saveFileDialog.FileName);
                    writer.WriteLine(txtInputBox.Text);
                    writer.Close();
                    break;
            }
        }
    }
}
