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
    /// FireAlarmWindow.xaml 的交互逻辑
    /// </summary>
    public partial class FireAlarmWindow : Window
    {        
        private readonly FireAlarmWindowViewModel viewModel;
        public FireAlarmWindow()
        {
            InitializeComponent();
            viewModel = new FireAlarmWindowViewModel();
            DataContext = viewModel;
        }

        private static FireAlarmWindow _instance;

        public static FireAlarmWindow Instance
        {
            get
            {
                if (_instance == null || !_instance.IsLoaded)
                    _instance = new FireAlarmWindow();
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
