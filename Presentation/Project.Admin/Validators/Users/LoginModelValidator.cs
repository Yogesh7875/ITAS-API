using FluentValidation;
using Project.Admin.Models.Users;
using Project.Services.Localization;
using Project.Web.Framework.Validators;

namespace Project.Admin.Validators.Users;

/// <summary>
/// Represents login model validator
/// </summary>
public class LoginModelValidator : BaseProjectValidator<LoginModel>
{
    #region Constructor

    public LoginModelValidator(ILocalizationService localizationService)
    {
        //RuleFor(x => x.Email)
        //    .NotEmpty()
        //    .WithMessageAwait(localizationService.GetResourceAsync("Admin.UserAuth.Code.Required"));

        //RuleFor(x => x.Password)
        //    .NotEmpty()
        //    .WithMessageAwait(localizationService.GetResourceAsync("Admin.UserAuth.Password.Required"));
    }

    #endregion
}
