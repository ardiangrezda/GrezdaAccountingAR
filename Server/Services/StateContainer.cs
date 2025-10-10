using Server.Models;

namespace Server.Services
{
    public class StateContainer
    {
        private List<SalesCategory>? _salesCategories;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly IServiceProvider _serviceProvider;

        public StateContainer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<List<SalesCategory>> GetSalesCategoriesAsync()
        {
            if (_salesCategories != null)
                return _salesCategories;

            await _semaphore.WaitAsync();
            try
            {
                if (_salesCategories != null)
                    return _salesCategories;

                using (var scope = _serviceProvider.CreateScope())
                {
                    var salesCategoryService = scope.ServiceProvider.GetRequiredService<SalesCategoryService>();
                    _salesCategories = await salesCategoryService.GetSalesCategoriesAsync();
                    return _salesCategories;
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task RefreshSalesCategoriesAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var salesCategoryService = scope.ServiceProvider.GetRequiredService<SalesCategoryService>();
                    _salesCategories = await salesCategoryService.GetSalesCategoriesAsync();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}