using LinqToDB;
using Project.Core;
using Project.Core.Domain.Master;
using Project.Data;

namespace Project.Services.Master;

public partial class VehicleCategoryService : IVehicleCategoryService
{
    private readonly IRepository<VehicleCategory> _repository;

    public VehicleCategoryService(IRepository<VehicleCategory> repository)
    {
        _repository = repository;
    }

    public async Task<IList<VehicleCategory>> GetAllVehicleCategoriesAsync(bool showHidden = false)
    {
        var query = _repository.Table.Where(c => !c.Deleted);
        if (!showHidden)
            query = query.Where(c => c.Active);

        return await query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id).ToListAsync();
    }

    public async Task<VehicleCategory> GetVehicleCategoryByIdAsync(long id) =>
        await _repository.GetByIdAsync(id);

    public async Task InsertVehicleCategoryAsync(VehicleCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        category.CreatedOnUtc = CommonHelper.IndianTimeNow;
        category.UpdatedOnUtc = CommonHelper.IndianTimeNow;
        await _repository.InsertAsync(category);
    }

    public async Task UpdateVehicleCategoryAsync(VehicleCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        category.UpdatedOnUtc = CommonHelper.IndianTimeNow;
        await _repository.UpdateAsync(category);
    }

    public async Task DeleteVehicleCategoryAsync(VehicleCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);
        await _repository.DeleteAsync(category);
    }
}
