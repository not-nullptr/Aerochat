using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Aerochat.ViewModels
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class DependsOnAttribute : Attribute
    {
        public string PropertyName { get; }

        public DependsOnAttribute(string propertyName)
        {
            PropertyName = propertyName;
        }
    }
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, newValue))
            {
                field = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                // get a list of properties that depend on this property using => syntax

                var dependentProperties = GetType().GetProperties()
                    .Where(p => p.GetCustomAttributes(typeof(DependsOnAttribute), true)
                        .Any(a => ((DependsOnAttribute)a).PropertyName == propertyName))
                    .Select(p => p.Name);

                foreach (var dependentProperty in dependentProperties)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(dependentProperty));
                }

                return true;
            }
            return false;
        }

        public void InvokePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
