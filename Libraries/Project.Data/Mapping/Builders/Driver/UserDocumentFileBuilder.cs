using FluentMigrator.Builders.Create.Table;
using Project.Core.Domain.Drivers;
using Project.Data.Extensions;

namespace Project.Data.Mapping.Builders.Drivers;

public class UserDocumentFileBuilder : ProjectEntityBuilder<UserDocumentFile>
{
    public override void MapEntity(CreateTableExpressionBuilder table) => table
        .WithColumn(nameof(UserDocumentFile.UserDocumentId)).AsInt64().NotNullable().ForeignKey<UserDocument>()
        .WithColumn(nameof(UserDocumentFile.StorageProvider)).AsString(30).NotNullable()
        .WithColumn(nameof(UserDocumentFile.StorageKey)).AsString(500).NotNullable()
        .WithColumn(nameof(UserDocumentFile.FileName)).AsString(255).NotNullable()
        .WithColumn(nameof(UserDocumentFile.ContentType)).AsString(100).NotNullable()
        .WithColumn(nameof(UserDocumentFile.CreatedOnUtc)).AsDateTime().NotNullable()
        .WithColumn(nameof(UserDocumentFile.CreatedBy)).AsInt64().NotNullable();
}
