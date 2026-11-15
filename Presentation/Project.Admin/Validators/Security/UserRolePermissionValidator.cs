using FluentValidation;
using Project.Admin.Models.Catalog;
using Project.Services.Localization;
using Project.Web.Framework.Validators;

namespace Project.Admin.Validators.Security;

/// <summary>
/// Represents user role permission validator
/// </summary>
public class UserRolePermissionValidator : BaseProjectValidator<UserRolePermissionModel>
{
    #region Constructor

    public UserRolePermissionValidator(ILocalizationService localizationService)
    {
        RuleFor(x => x.Id)
            .Must(x => x > 0)
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.Catalog.UserRole.Permissions.RoleId.Required"));
    }

    #endregion
}
