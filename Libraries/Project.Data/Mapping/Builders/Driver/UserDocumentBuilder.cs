using FluentMigrator.Builders.Create.Table;
using Project.Core.Domain.Drivers;
using Project.Core.Domain.Master;
using Project.Core.Domain.Users;
using Project.Data.Extensions;

namespace Project.Data.Mapping.Builders.Drivers;

public class UserDocumentBuilder : ProjectEntityBuilder<UserDocument>
{
    public override void MapEntity(CreateTableExpressionBuilder table) => table
        .WithColumn(nameof(UserDocument.UserId)).AsInt64().NotNullable().ForeignKey<User>()
        .WithColumn(nameof(UserDocument.DocumentTypeId)).AsInt64().NotNullable().ForeignKey<DocumentType>()
        .WithColumn(nameof(UserDocument.DocumentNumber)).AsString(100).Nullable()
        .WithColumn(nameof(UserDocument.RejectionReason)).AsString(500).Nullable()
        .WithColumn(nameof(UserDocument.CreatedOnUtc)).AsDateTime().NotNullable()
        .WithColumn(nameof(UserDocument.CreatedBy)).AsInt64().NotNullable()
        .WithColumn(nameof(UserDocument.UpdatedOnUtc)).AsDateTime().Nullable()
        .WithColumn(nameof(UserDocument.UpdatedBy)).AsInt64().Nullable();
}
