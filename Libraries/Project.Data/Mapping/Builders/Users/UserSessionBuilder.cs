using System.Data;
using FluentMigrator.Builders.Create.Table;
using Project.Core.Domain.Catalog;
using Project.Core.Domain.Users;
using Project.Data.Extensions;

namespace Project.Data.Mapping.Builders.Users;

public class UserSessionBuilder : ProjectEntityBuilder<UserSession>
{
    #region Methods
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(UserSession.Id)).AsInt64().PrimaryKey().Identity()
            .WithColumn(nameof(UserSession.UserId)).AsInt64().NotNullable().ForeignKey<User>()
            .WithColumn(nameof(UserSession.SessionId)).AsGuid().NotNullable()
            .WithColumn(nameof(UserSession.RefreshTokenHash)).AsString(500).NotNullable()
            .WithColumn(nameof(UserSession.PreviousRefreshTokenHash)).AsString(500).Nullable()
            .WithColumn(nameof(UserSession.CreatedOnUtc)).AsDateTime().NotNullable()
            .WithColumn(nameof(UserSession.LastRefreshedOnUtc)).AsDateTime().Nullable()
            .WithColumn(nameof(UserSession.ExpiresAtUtc)).AsDateTime().NotNullable()
            .WithColumn(nameof(UserSession.RevokedOnUtc)).AsDateTime().Nullable()
            .WithColumn(nameof(UserSession.RevokedReason)).AsString(500).Nullable()
            .WithColumn(nameof(UserSession.DeviceId)).AsString(250).Nullable()
            .WithColumn(nameof(UserSession.DeviceName)).AsString(250).Nullable()
            .WithColumn(nameof(UserSession.IpAddress)).AsString(50).Nullable()
            .WithColumn(nameof(UserSession.UserAgent)).AsString(500).Nullable()
            .WithColumn(nameof(UserSession.AppType)).AsString(50).NotNullable().WithDefaultValue("default")
            .WithColumn(nameof(UserSession.IsSingleDevice)).AsBoolean().NotNullable().WithDefaultValue(true);

    }
    #endregion
}
