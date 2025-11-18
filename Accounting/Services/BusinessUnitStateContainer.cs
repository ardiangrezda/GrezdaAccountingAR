using Microsoft.EntityFrameworkCore;
using Accounting.Data;
using Accounting.Models;

namespace Accounting.Services
{
    public class BusinessUnitStateContainer
    {
        private int? _currentBusinessUnitId;
        public event Action? OnChange;

        public int? CurrentBusinessUnitId
        {
            get => _currentBusinessUnitId;
            set
            {
                _currentBusinessUnitId = value;
                NotifyStateChanged();
            }
        }

        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}