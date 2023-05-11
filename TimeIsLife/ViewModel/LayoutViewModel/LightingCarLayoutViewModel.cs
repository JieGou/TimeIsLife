using CommunityToolkit.Mvvm.ComponentModel;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeIsLife.ViewModel.LayoutViewModel
{
    internal class LightingCarLayoutViewModel : ObservableObject
    {
        public static LightingCarLayoutViewModel Instance { get; set; }
        public LightingCarLayoutViewModel()
        {

        }
    }
}
