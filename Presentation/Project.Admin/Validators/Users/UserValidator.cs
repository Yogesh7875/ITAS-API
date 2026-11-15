using FluentValidation;
using Project.Admin.Models.Users;
using Project.Services.Localization;
using Project.Web.Framework.Validators;

namespace Project.Admin.Validators.Users;

/// <summary>
/// Represents user validator
/// </summary>
public partial class UserValidator : BaseProjectValidator<UserModel>
{
    #region Constructor

    public UserValidator(ILocalizationService localizationService)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.User.Name.Required"));

        RuleFor(x => x.EmailAddress)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.User.EmailAddress.Required"));

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.User.Password.Required"));

        RuleFor(x => x.RoleId)
            .Must(x => x > 0)
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.User.RoleId.Required"));

        
    }

    #endregion
}
