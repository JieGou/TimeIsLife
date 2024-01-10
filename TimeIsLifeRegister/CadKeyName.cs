using CommunityToolkit.Mvvm.ComponentModel;

namespace TimeIsLifeRegister
{
    public class CadKeyName : ObservableObject
    {
        public CadKeyName(string name, string key,bool isChecked)
        {
            Name = name;
            Key = key;
            IsChecked = isChecked;
        }

        private string? name;
        public string? Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        private string? key;
        public string? Key
        {
            get => key;
            set => SetProperty(ref key, value);
        }

        private bool isChecked;
        public bool IsChecked
        {
            get => isChecked;
            set => SetProperty(ref isChecked, value);
        }
    }
}
