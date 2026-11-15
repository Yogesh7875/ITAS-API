using FluentValidation;
using Project.Admin.Models.Catalog;
using Project.Services.Localization;
using Project.Web.Framework.Validators;

namespace Project.Admin.Validators.Catalog;

/// <summary>
/// Represents location validator
/// </summary>
public class LocationValidator : BaseProjectValidator<LocationModel>
{
    #region Constructor

    public LocationValidator(ILocalizationService localizationService)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.Catalog.Location.Name.Required"));

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.Catalog.Location.Code.Required"));
    }

    #endregion
}
