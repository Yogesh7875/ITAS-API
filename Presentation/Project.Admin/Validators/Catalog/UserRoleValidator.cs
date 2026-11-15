using FluentValidation;
using Project.Admin.Models.Catalog;
using Project.Services.Localization;
using Project.Web.Framework.Validators;

namespace Project.Admin.Validators.Catalog;

/// <summary>
/// Represents user role validator
/// </summary>
public class UserRoleValidator : BaseProjectValidator<UserRoleModel>
{
    #region Constructor

    public UserRoleValidator(ILocalizationService localizationService)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.Catalog.UserRole.Name.Required"));
    }

    #endregion
}
