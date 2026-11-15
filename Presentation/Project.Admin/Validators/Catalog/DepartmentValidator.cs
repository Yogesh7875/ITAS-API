using FluentValidation;
using Project.Admin.Models.Catalog;
using Project.Services.Localization;
using Project.Web.Framework.Validators;

namespace Project.Admin.Validators.Catalog;

/// <summary>
/// Represents Department validator
/// </summary>
public class DepartmentValidator : BaseProjectValidator<DepartmentModel>
{
    #region Constructor

    public DepartmentValidator(ILocalizationService localizationService)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.Catalog.Department.Name.Required"));

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessageAwait(localizationService.GetResourceAsync("Admin.Catalog.Department.Code.Required"));
    }

    #endregion
}
