using FluentMigrator.Builders.Create.Table;
using Project.Core.Domain.Master;

namespace Project.Data.Mapping.Builders.Master;

public class VehicleCategoryBuilder : ProjectEntityBuilder<VehicleCategory>
{
    public override void MapEntity(CreateTableExpressionBuilder table) => table
        .WithColumn(nameof(VehicleCategory.Name)).AsString(100).NotNullable()
        .WithColumn(nameof(VehicleCategory.Seats)).AsInt32().NotNullable().WithDefaultValue(4)
        .WithColumn(nameof(VehicleCategory.DisplayOrder)).AsInt32().NotNullable().WithDefaultValue(0)
        .WithColumn(nameof(VehicleCategory.Active)).AsBoolean().NotNullable().WithDefaultValue(true)
        .WithColumn(nameof(VehicleCategory.Deleted)).AsBoolean().NotNullable().WithDefaultValue(false)
        .WithColumn(nameof(VehicleCategory.CreatedOnUtc)).AsDateTime().NotNullable()
        .WithColumn(nameof(VehicleCategory.CreatedBy)).AsInt64().NotNullable()
        .WithColumn(nameof(VehicleCategory.UpdatedOnUtc)).AsDateTime().Nullable()
        .WithColumn(nameof(VehicleCategory.UpdatedBy)).AsInt64().Nullable();
}
