using System.Data;
using Project.Data.Extensions;
using FluentMigrator.Builders.Create.Table;
using Project.Core.Domain.Users;
using Project.Core.Domain.Security;

namespace Project.Data.Mapping.Builders.Users;

/// <summary>
/// Represents user permission record mapping builder
/// </summary>
public class UserPermissionRecordMappingBuilder : ProjectEntityBuilder<UserPermissionRecordMapping>
{
    #region Methods

    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(UserPermissionRecordMapping.UserId)).AsInt64().PrimaryKey().ForeignKey<User>().OnDeleteOrUpdate(Rule.None)
            .WithColumn(nameof(UserPermissionRecordMapping.PermissionRecordId)).AsInt64().PrimaryKey().ForeignKey<PermissionRecord>().OnDeleteOrUpdate(Rule.None);
    }

    #endregion
}
