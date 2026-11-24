using FluentMigrator.Builders.Create.Table;
using Project.Core.Domain.Drivers;
using Project.Core.Domain.Master;
using Project.Core.Domain.Users;
using Project.Data.Extensions;

namespace Project.Data.Mapping.Builders.Drivers;

public class DriverVehicleBuilder : ProjectEntityBuilder<DriverVehicle>
{
    public override void MapEntity(CreateTableExpressionBuilder table) => table
        .WithColumn(nameof(DriverVehicle.UserId)).AsInt64().NotNullable().ForeignKey<User>()
        .WithColumn(nameof(DriverVehicle.VehicleCategoryId)).AsInt64().NotNullable().ForeignKey<VehicleCategory>()
        .WithColumn(nameof(DriverVehicle.RegistrationNumber)).AsString(20).NotNullable()
        .WithColumn(nameof(DriverVehicle.Make)).AsString(100).Nullable()
        .WithColumn(nameof(DriverVehicle.Model)).AsString(100).Nullable()
        .WithColumn(nameof(DriverVehicle.Color)).AsString(50).Nullable()
        .WithColumn(nameof(DriverVehicle.CreatedOnUtc)).AsDateTime().NotNullable()
        .WithColumn(nameof(DriverVehicle.CreatedBy)).AsInt64().NotNullable()
        .WithColumn(nameof(DriverVehicle.UpdatedOnUtc)).AsDateTime().Nullable()
        .WithColumn(nameof(DriverVehicle.UpdatedBy)).AsInt64().Nullable();
}
