using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TimeIsLife.ViewModel;

namespace TimeIsLife.View
{
    /// <summary>
    /// F9_Window.xaml 的交互逻辑
    /// </summary>
    public partial class F9_Window : Window
    {
        private readonly F9_WindowViewModel viewModel;
        public F9_Window()
        {
            InitializeComponent();
            viewModel = new F9_WindowViewModel();
            this.DataContext = viewModel;
        }

        private static F9_Window _instance;

        public static F9_Window Instance
        {
            get
            {
                if (_instance == null || !_instance.IsLoaded)
                    _instance = new F9_Window();
                return _instance;
            }
        }


        private void Window_Closing(object sender, CancelEventArgs e)
        {
            // 确保状态保存操作是安全的
            try
            {
                viewModel.SaveState();
            }
            catch (Exception ex)
            {
                // 处理异常，如记录日志
                MessageBox.Show("发生错误: " + ex.Message); // 提供详细的错误信息
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保状态加载操作是安全的
            try
            {
                viewModel.LoadState();
            }
            catch (Exception ex)
            {
                // 处理异常，如记录日志
                MessageBox.Show("发生错误: " + ex.Message); // 提供详细的错误信息
            }
        }
    }
}
