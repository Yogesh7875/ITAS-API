using FluentMigrator;
using Project.Core.Domain.Drivers;
using Project.Core.Domain.Master;
using Project.Core.Domain.Users;
using Project.Data.Extensions;

namespace Project.Data.Migrations.Installation;


[ProjectSchemaMigration("2026/11/22 18:50:16:8799999", "Nop.Data base schema", MigrationProcessType.Installation)]
public class SchemaMigration : ForwardOnlyMigration
{
    
    /// <summary>
    /// Collect the UP migration expressions
    /// <remarks>
    /// We use an explicit table creation order instead of an automatic one
    /// due to problems creating relationships between tables
    /// </remarks>
    /// </summary>
    public override void Up()
    {
        //Create.TableFor<Setting>();
        //Create.TableFor<UserRole>();
        //Create.TableFor<User>();
        //Create.TableFor<ScheduleTask>();

        //Create.TableFor<Language>();
        //Create.TableFor<Log>();

        //Create.TableFor<LocaleStringResource>();
        //Create.TableFor<UserSession>();

        //    Alter.Table(nameof(PurchaseOrder))
        //    .AddColumn(nameof(PurchaseOrder.CustomOrderId)).AsInt64().Nullable().ForeignKey<CustomOrder>();

        //Alter.Table(nameof(User))
        //    .AddColumn(nameof(User.Dob)).AsDateTime().Nullable()
        //    .AddColumn(nameof(User.Gender)).AsString(50).Nullable()
        //    .AddColumn(nameof(User.Address)).AsString(500).Nullable()
        //    .AddColumn(nameof(User.Fcmtoken)).AsString(500).Nullable()
        //    .AddColumn(nameof(User.OTP)).AsString(10).Nullable();

        //Create.TableFor<UserSession>();
        Create.TableFor<Company>();
        Alter.Table(nameof(User))
            .AddColumn(nameof(User.CompanyId)).AsInt64().Nullable().ForeignKey<Company>()
            .AddColumn(nameof(User.ApprovalStatusId)).AsInt32().NotNullable().WithDefaultValue(1);

        Create.TableFor<DocumentType>();
        Create.TableFor<VehicleCategory>();
        Create.TableFor<DriverProfile>();
        Create.TableFor<DriverVehicle>();
        Create.TableFor<UserDocument>();
        Create.TableFor<UserDocumentFile>();
    }
}