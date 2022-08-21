using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.ViewModel
{
    class ElectricalViewModel
    {
        #region 计算电流

        //额定功率
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Pc))]
        public string pe;

        //需要系数
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Pc))]
        public string kx;

        //计算有功功率
        public string Pc 
        {
            get => $"{pe}{kx}"; 
        }

        //计算无功功率
        public string Qc { get; set; }

        //功率因数
        [ObservableProperty]
        public string cosø;

        //计算电流
        [ObservableProperty]
        public string ic;

        [ObservableProperty]
        public string lj;

        [ObservableProperty]
        public string ln;



        #endregion
    }
}
