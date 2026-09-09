using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace APP.ViewModels
{
    public class StatisticsViewModel : INotifyPropertyChanged
    {
        public List<string> Statistics = new List<string>();

        public bool HasStatistics => Statistics.Count > 0;
        public bool HasNoStatistics => !HasStatistics;
        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyStatisticsChanged()
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(HasStatistics))
                );

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(HasNoStatistics))
                );
        }
    }
}
