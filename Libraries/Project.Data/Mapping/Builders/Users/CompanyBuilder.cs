using FluentMigrator.Builders.Create.Table;
using Project.Core.Domain.Users;

namespace Project.Data.Mapping.Builders.Users;

public class CompanyBuilder : ProjectEntityBuilder<Company>
{
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(Company.Name)).AsString(250).NotNullable()
            .WithColumn(nameof(Company.CreatedOn)).AsDateTime().NotNullable()
            .WithColumn(nameof(Company.UpdatedOn)).AsDateTime().Nullable()
            .WithColumn(nameof(Company.CreatedBy)).AsInt64().NotNullable()
            .WithColumn(nameof(Company.UpdatedBy)).AsInt64().Nullable();
    }
}
