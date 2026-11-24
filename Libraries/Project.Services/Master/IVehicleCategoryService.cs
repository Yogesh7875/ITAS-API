using Project.Core.Domain.Master;

namespace Project.Services.Master;

public partial interface IVehicleCategoryService
{
    Task<IList<VehicleCategory>> GetAllVehicleCategoriesAsync(bool showHidden = false);

    Task<VehicleCategory> GetVehicleCategoryByIdAsync(long id);

    Task InsertVehicleCategoryAsync(VehicleCategory category);

    Task UpdateVehicleCategoryAsync(VehicleCategory category);

    Task DeleteVehicleCategoryAsync(VehicleCategory category);
}
