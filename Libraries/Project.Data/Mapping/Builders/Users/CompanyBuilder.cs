using FluentMigrator.Builders.Create.Table;
using Project.Core.Domain.Users;

namespace Project.Data.Mapping.Builders.Users;

public class CompanyBuilder : ProjectEntityBuilder<Company>
{

    public override void MapEntity(CreateTableExpressionBuilder table) => table
    .WithColumn(nameof(Company.Code)).AsString(50).NotNullable()
    .WithColumn(nameof(Company.Name)).AsString(250).NotNullable()
    .WithColumn(nameof(Company.EmailDomain)).AsString(250).NotNullable()
    .WithColumn(nameof(Company.CreditLimit)).AsDecimal(18, 2).NotNullable().WithDefaultValue(0)
    .WithColumn(nameof(Company.BillingCycleDays)).AsInt32().NotNullable().WithDefaultValue(30)
    .WithColumn(nameof(Company.Remark)).AsString(500).Nullable();

}
