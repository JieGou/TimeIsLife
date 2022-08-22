using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestApp
{
    internal class MainWindowViewModel: ObservableObject
    {
        public MainWindowViewModel()
        {
            CalculateCurrentCommand = new RelayCommand(CalculateCurrent);
        }
        #region 计算电流

        //额定功率
        private string pe;
        public string Pe
        {
            get => pe;
            set => SetProperty(ref pe, value);

        }

        //需要系数
        private string kx;
        public string Kx
        {
            get => kx;
            set => SetProperty(ref kx, value);
        }

        //功率因数
        private string cosø;
        public string Cosø
        {
            get => cosø;
            set => SetProperty(ref cosø, value);
        }

        //计算电流
        private string ic;
        public string Ic
        {
            get => ic;
            set => SetProperty(ref ic, value);
        }

        
        public IRelayCommand CalculateCurrentCommand { get;  }

        void CalculateCurrent()
        {
            if (string.IsNullOrEmpty(pe) || string.IsNullOrEmpty(kx) || string.IsNullOrEmpty(cosø)) return;

            Ic = Math.Round((double.Parse(pe) * double.Parse(kx)) / (Math.Sqrt(3) * double.Parse(cosø)), 2).ToString();
        }

        #endregion
    }
}
