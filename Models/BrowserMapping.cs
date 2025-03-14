using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DefaultBrowser.Models
{
    public class BrowserMapping : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private string _name = string.Empty;
        private string _pattern = string.Empty;
        private string _browserPath = string.Empty;
        private int _order;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Pattern
        {
            get => _pattern;
            set => SetProperty(ref _pattern, value);
        }

        public string BrowserPath
        {
            get => _browserPath;
            set => SetProperty(ref _browserPath, value);
        }

        public int Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(field, value))
                return false;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public bool IsValidPattern()
        {
            try
            {
                // Try to create a regex from the pattern to validate it
                var regex = new Regex(Pattern, RegexOptions.IgnoreCase);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool MatchesUrl(string url)
        {
            if (string.IsNullOrEmpty(Pattern) || string.IsNullOrEmpty(url))
                return false;

            try
            {
                return Regex.IsMatch(url, Pattern, RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }
        
        // Display name for the browser derived from the path
        [JsonIgnore]
        public string DisplayBrowserName 
        { 
            get
            {
                // Get the display name from the platform service which maintains a list of browsers
                return PlatformService.Instance.GetFriendlyBrowserName(BrowserPath);
            }
        }
    }
}