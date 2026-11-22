using Microsoft.AspNetCore.Mvc;
using Project.Admin.Models.Users;
using Project.Core;
using Project.Services.Users;
using Project.Web.Framework;

namespace Project.Admin.Controllers;

public class UserController : BaseProtectedController
{
    #region Fields

    private readonly IUserService _userService;
    private readonly IWorkContext _workContext;

    #endregion

    #region Constructor

    public UserController(IUserService userService, IWorkContext workContext)
    {
        _userService = userService;
        _workContext = workContext;
    }

    #endregion

    #region Methods
    [HttpGet]
    public async Task<IActionResult> GetCustomerPersonalDetails()
    {
        var workUser = await _workContext.GetCurrentUserAsync();
        if (workUser == null)
        {
            return Error(string.Empty, "User not found");
        }
        var user = await _userService.GetUserByIdAsync(workUser.Id);

        if (user == null)
        {
            return Error(string.Empty, "User not found");
        }

        var record = new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.EmailAddress
        };

        return Success(record, "Success");
    }
    [HttpPost]
    public async Task<IActionResult> UpdateUserPersonalDetail(UserPersonalDetails model)
    {
        var user = await _userService.GetUserByIdAsync(model.Id);
        if (user == null)
        {
            return Error(string.Empty, "User not found");
        }
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.EmailAddress = model.EmailAddress;
        user.UpdatedOnUtc = DateTime.UtcNow;
        await _userService.UpdateUserAsync(user);
        return Success(string.Empty, "User updated successfuly");
    }
    [HttpGet]
    public async Task<IActionResult>GetDriverUser(long id)
    {
        var workUser = await _workContext.GetCurrentUserAsync();
        if (workUser == null)
        {
            return Error(string.Empty, "User not found");
        }
        var user = await _userService.GetUserByIdAsync(workUser.Id);

        if (user == null)
        {
            return Error(string.Empty, "User not found");
        }

        var record = new
        {
            user.Id,
            user.FirstName,
            user.LastName,
            user.EmailAddress,
            user.Dob,
            user.Address,
            user.Gender,
        };

        return Success(record, "Success");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateDriverPersonalDetails(UserModel model)
    {
        var user = await _userService.GetUserByIdAsync(model.Id);
        if (user == null)
        {
            return Error(string.Empty, "User not found");
        }
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.EmailAddress = model.LastName;
        user.Dob = model.Dob;
        user.Gender = model.Gender;
        user.Address = model.Address;
        user.UpdatedOnUtc = DateTime.UtcNow;
        await _userService.UpdateUserAsync(user);
        return Success(string.Empty, "Driver updated successfuly");
    }
    #endregion
}
