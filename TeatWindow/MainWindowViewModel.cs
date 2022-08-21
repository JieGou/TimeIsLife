using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestWindow
{
    
    public partial class MainWindowViewModel:ObservableObject
    {
        //额定功率
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Pc))]
        public string pe;

        //需要系数
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Pc))]
        public string kx;

        //计算有功功率
        public string Pc => $"{Pe}{Kx}";
    }
}
