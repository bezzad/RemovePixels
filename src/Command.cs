using System;
using System.Windows.Input;

namespace RemovePixel
{
    public class Command : ICommand
    {
        public Command(Action execute = null, Func<bool> canExecute = null)
        {
            ExecuteMethod = execute ?? throw new ArgumentNullException(nameof(execute));
            CanExecuteMethod = canExecute;
        }


        private Action ExecuteMethod { get; }
        private Func<bool> CanExecuteMethod { get; }
        private bool _isExecuting;
        public bool IsExecuting
        {
            get => _isExecuting;
            set
            {
                if (_isExecuting != value)
                {
                    _isExecuting = value;
                    RaiseCanExecuteChanged();
                }
            }
        }
        public event EventHandler CanExecuteChanged;


        public bool CanExecute(object parameter)
        {
            return !IsExecuting && (CanExecuteMethod?.Invoke() ?? true);
        }

        public void Execute(object parameter)
        {
            if (!CanExecute(parameter)) return;

            IsExecuting = true;
            try
            {
                ExecuteMethod?.Invoke();
            }
            finally
            {
                IsExecuting = false;
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

    }
}
