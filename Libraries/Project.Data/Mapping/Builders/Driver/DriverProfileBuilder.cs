using FluentMigrator.Builders.Create.Table;
using Project.Core.Domain.Drivers;
using Project.Core.Domain.Master;
using Project.Core.Domain.Users;
using Project.Data.Extensions;

namespace Project.Data.Mapping.Builders.Drivers;

public class DriverProfileBuilder : ProjectEntityBuilder<DriverProfile>
{
    public override void MapEntity(CreateTableExpressionBuilder table) => table
        .WithColumn(nameof(DriverProfile.UserId)).AsInt64().NotNullable().ForeignKey<User>()
        .WithColumn(nameof(DriverProfile.City)).AsString(100).Nullable()
        .WithColumn(nameof(DriverProfile.OnboardingStatusId)).AsInt32().NotNullable().WithDefaultValue(0)
        .WithColumn(nameof(DriverProfile.SubmittedOnUtc)).AsDateTime().Nullable()
        .WithColumn(nameof(DriverProfile.ReviewedOnUtc)).AsDateTime().Nullable()
        .WithColumn(nameof(DriverProfile.ReviewedByUserId)).AsInt64().Nullable().ForeignKey<User>()
        .WithColumn(nameof(DriverProfile.RejectionReason)).AsString(500).Nullable()
        .WithColumn(nameof(DriverProfile.Deleted)).AsBoolean().NotNullable().WithDefaultValue(false)
        .WithColumn(nameof(DriverProfile.CreatedOnUtc)).AsDateTime().NotNullable()
        .WithColumn(nameof(DriverProfile.CreatedBy)).AsInt64().NotNullable()
        .WithColumn(nameof(DriverProfile.UpdatedOnUtc)).AsDateTime().Nullable()
        .WithColumn(nameof(DriverProfile.UpdatedBy)).AsInt64().Nullable();
}
