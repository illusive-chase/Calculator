using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace UI
{
    //用ScrollViewer实现滚动条
    public class SmoothScrollViewer : ScrollViewer
    {
        public double VerticalScrollAmount
        {
            get { return (double)GetValue(VerticalScrollAmountProperty); }
            set { SetValue(VerticalScrollAmountProperty, value); }
        }


        //注册VerticalScrollAmountProperty依赖属性，有效值更改时使用回调函数V_ScrollAmountChangedCallBack
        public static readonly DependencyProperty VerticalScrollAmountProperty =
            DependencyProperty.Register("VerticalScrollAmount", typeof(double), typeof(SmoothScrollViewer), new PropertyMetadata(0.0, new PropertyChangedCallback(V_ScrollAmountChangedCallBack)));

        //将内容滑动到指定位置
        private static void V_ScrollAmountChangedCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var scrollViewer = d as SmoothScrollViewer;
            scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
            //var scroller = VisualTreeHelper.GetParent(scrollViewer.Content as DependencyObject) as ScrollContentPresenter;
            //scroller.SetVerticalOffset((double)e.NewValue);
        }

    }

}
