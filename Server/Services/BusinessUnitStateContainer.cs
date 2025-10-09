using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;

namespace Server.Services
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