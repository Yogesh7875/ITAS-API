using Project.Core.Domain.Master;

namespace Project.Services.Master;

public partial interface IVehicleCategoryService
{
    Task<IList<VehicleCategory>> GetAllVehicleCategoriesAsync(bool showHidden = false);

    Task<VehicleCategory> GetVehicleCategoryByIdAsync(long id);

    /// <summary>Not-deleted category with this name (case-insensitive), or null.</summary>
    Task<VehicleCategory> GetVehicleCategoryByNameAsync(string name);

    /// <summary>Number of (not deleted) driver vehicles per category id.</summary>
    Task<IDictionary<long, int>> GetVehicleCountsAsync();

    Task InsertVehicleCategoryAsync(VehicleCategory category);

    Task UpdateVehicleCategoryAsync(VehicleCategory category);

    Task DeleteVehicleCategoryAsync(VehicleCategory category);
}
