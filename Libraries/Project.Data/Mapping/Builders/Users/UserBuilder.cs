using System.Data;
using FluentMigrator.Builders.Create.Table;
using Project.Core.Domain.Catalog;
using Project.Core.Domain.Users;
using Project.Data.Extensions;

namespace Project.Data.Mapping.Builders.Users;

/// <summary>
/// Represents user builder
/// </summary>
public class UserBuilder : ProjectEntityBuilder<User>
{
    #region Methods

    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(User.FirstName)).AsString(250).Nullable()
            .WithColumn(nameof(User.LastName)).AsString(250).Nullable()
            .WithColumn(nameof(User.Token)).AsString(500).Nullable()
            .WithColumn(nameof(User.Remark)).AsString(500).Nullable()
            .WithColumn(nameof(User.Password)).AsString(250).Nullable()
            .WithColumn(nameof(User.EmailAddress)).AsString(250).Nullable()
            .WithColumn(nameof(User.MobileNumber)).AsString(50).NotNullable()
            .WithColumn(nameof(User.BannedOnUtc)).AsDateTime().Nullable()
            .WithColumn(nameof(User.IsBanned)).AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn(nameof(User.Fcmtoken)).AsString(500).Nullable()
            .WithColumn(nameof(User.Dob)).AsDateTime().Nullable()
            .WithColumn(nameof(User.Gender)).AsString(50).Nullable()
            .WithColumn(nameof(User.Address)).AsString(500).Nullable()
            .WithColumn(nameof(User.OTP)).AsString(10).Nullable()
            .WithColumn(nameof(User.CompanyId)).AsInt64().Nullable().ForeignKey<Company>().OnDeleteOrUpdate(Rule.None)
            .WithColumn(nameof(User.ApprovalStatusId)).AsInt32().NotNullable().WithDefaultValue(true)
            .WithColumn(nameof(User.RoleId)).AsInt64().NotNullable().ForeignKey<UserRole>().OnDeleteOrUpdate(Rule.None);
    }

    #endregion
}