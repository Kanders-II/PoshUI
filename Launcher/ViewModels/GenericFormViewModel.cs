// Copyright (c) 2025 A Solution IT LLC. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Launcher.Services;

namespace Launcher.ViewModels
{
    public class GenericFormViewModel : INotifyPropertyChanged
    {
        private string _title;
        private string _description;
        private ObservableCollection<ParameterViewModel> _parameters = new ObservableCollection<ParameterViewModel>();
        private ObservableCollection<CardViewModel> _additionalCards = new ObservableCollection<CardViewModel>();
        private ObservableCollection<BannerViewModel> _banners = new ObservableCollection<BannerViewModel>();

        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ParameterViewModel> Parameters
        {
            get => _parameters;
            set { _parameters = value; OnPropertyChanged(); }
        }

        public ObservableCollection<CardViewModel> AdditionalCards
        {
            get => _additionalCards;
            set { _additionalCards = value; OnPropertyChanged(); }
        }

        public ObservableCollection<BannerViewModel> Banners
        {
            get => _banners;
            set { _banners = value; OnPropertyChanged(); }
        }

        public GenericFormViewModel(string title, string description = "")
        {
            LoggingService.Trace($"Creating GenericFormViewModel with title: '{title}', description: '{description}'");
            _title = title;
            _description = description;
            LoggingService.Trace($"GenericFormViewModel created with title: '{_title}', description: '{_description}'");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 