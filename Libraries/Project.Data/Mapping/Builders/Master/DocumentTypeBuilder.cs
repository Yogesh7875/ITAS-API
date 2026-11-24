using FluentMigrator.Builders.Create.Table;
using Project.Core.Domain.Master;

namespace Project.Data.Mapping.Builders.Master;

public class DocumentTypeBuilder : ProjectEntityBuilder<DocumentType>
{
    public override void MapEntity(CreateTableExpressionBuilder table) => table
        .WithColumn(nameof(DocumentType.Code)).AsString(50).NotNullable()
        .WithColumn(nameof(DocumentType.Name)).AsString(200).NotNullable()
        .WithColumn(nameof(DocumentType.Description)).AsString(500).Nullable()
        .WithColumn(nameof(DocumentType.AppliesToId)).AsInt32().NotNullable().WithDefaultValue(1)
        .WithColumn(nameof(DocumentType.IsMandatory)).AsBoolean().NotNullable().WithDefaultValue(true)
        .WithColumn(nameof(DocumentType.RequiresNumber)).AsBoolean().NotNullable().WithDefaultValue(false)
        .WithColumn(nameof(DocumentType.NumberLabel)).AsString(100).Nullable()
        .WithColumn(nameof(DocumentType.NumberRegex)).AsString(200).Nullable()
        .WithColumn(nameof(DocumentType.RequiresExpiry)).AsBoolean().NotNullable().WithDefaultValue(false)
        .WithColumn(nameof(DocumentType.FilesRequired)).AsInt32().NotNullable().WithDefaultValue(1)
        .WithColumn(nameof(DocumentType.AllowedFileTypes)).AsString(100).NotNullable()
        .WithColumn(nameof(DocumentType.MaxFileSizeKb)).AsInt32().NotNullable().WithDefaultValue(5120)
        .WithColumn(nameof(DocumentType.DisplayOrder)).AsInt32().NotNullable().WithDefaultValue(0)
        .WithColumn(nameof(DocumentType.Active)).AsBoolean().NotNullable().WithDefaultValue(true)
        .WithColumn(nameof(DocumentType.Deleted)).AsBoolean().NotNullable().WithDefaultValue(false)
        .WithColumn(nameof(DocumentType.CreatedOnUtc)).AsDateTime().NotNullable()
        .WithColumn(nameof(DocumentType.CreatedBy)).AsInt64().NotNullable()
        .WithColumn(nameof(DocumentType.UpdatedOnUtc)).AsDateTime().Nullable()
        .WithColumn(nameof(DocumentType.UpdatedBy)).AsInt64().Nullable();
}
