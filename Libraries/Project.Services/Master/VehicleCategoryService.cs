using LinqToDB;
using Project.Core;
using Project.Core.Domain.Drivers;
using Project.Core.Domain.Master;
using Project.Data;

namespace Project.Services.Master;

public partial class VehicleCategoryService : IVehicleCategoryService
{
    private readonly IRepository<VehicleCategory> _repository;
    private readonly IRepository<DriverVehicle> _vehicleRepository;

    public VehicleCategoryService(IRepository<VehicleCategory> repository,
        IRepository<DriverVehicle> vehicleRepository)
    {
        _repository = repository;
        _vehicleRepository = vehicleRepository;
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

    public async Task<VehicleCategory> GetVehicleCategoryByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalized = name.Trim().ToLower();
        return await _repository.Table.FirstOrDefaultAsync(c => !c.Deleted && c.Name.ToLower() == normalized);
    }

    public async Task<IDictionary<long, int>> GetVehicleCountsAsync()
    {
        var rows = await _vehicleRepository.Table
            .Where(v => !v.Deleted)
            .GroupBy(v => v.VehicleCategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync();

        return rows.ToDictionary(r => r.CategoryId, r => r.Count);
    }

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
