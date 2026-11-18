using System.Data;
using LinqToDB;
using Project.Core.Domain.Catalog;
using Project.Core.Domain.Users;
using Project.Data;
using Project.Services.Users.DataModels;

namespace Project.Services.Users;

/// <summary>
/// Represents user service
/// </summary>
public class UserService : IUserService
{
    #region Fields

    private readonly IRepository<User> _userRepository;
    private readonly IRepository<UserRole> _userRoleRepository;


    #endregion

    #region Constructor

    public UserService(IRepository<User> userRepository,
        IRepository<UserRole> userRoleRepository)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;

    }

    #endregion

    #region Methods

    /// <summary>
    ///  Gets a user by code
    /// </summary>
    /// <param name="code"></param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the User
    /// </returns>
    public async Task<User> GetUserByUserNameAsync(string email, long id)
    {
        return await _userRepository.Table.FirstOrDefaultAsync(c => c.EmailAddress == email && c.Id != id && c.Deleted == false);
    }

    /// <summary>
    /// Gets a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user
    /// </returns>
    public async Task<User> GetUserByIdAsync(long userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    /// <summary>
    /// Validate user name and password
    /// </summary>
    /// <param name="code">Username</param>
    /// <param name="password">Password</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user
    /// </returns>
    public async Task<User> ValidateUserNameAndPasswordAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentNullException(nameof(email));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentNullException(nameof(password));
        // add by avinash for login with universe database password
        var hashPassoword = KeyPasswordSet(password);
        var query = _userRepository.Table;
        var user = await query.FirstOrDefaultAsync(user => user.EmailAddress == email && user.Password == password || user.Password == hashPassoword && user.Deleted == false);

        return user;
    }
    /// <summary>
    /// Converts a string to a custom encoded format by converting each character to its ASCII value,
    /// padding it to three digits, and then reversing the entire string.
    /// </summary>
    /// <param name="sValue">The input string to encode.</param>
    /// <returns>The encoded string.</returns>
    public string KeyPasswordSet(string sValue)
    {
        string strR = "";
        foreach (char c in sValue)
        {
            int intTemp = (int)c;
            string strTemp1;

            if (intTemp <= 9)
                strTemp1 = "00";
            else if (intTemp <= 99)
                strTemp1 = "0";
            else
                strTemp1 = "";

            strR += strTemp1 + intTemp.ToString();
        }

        return new string(strR.Reverse().ToArray());
    }
    /// <summary>
    /// Decodes a custom encoded string by reversing the string, splitting it into three-digit ASCII values,
    /// and converting those values back to characters.
    /// </summary>
    /// <param name="sValue">The encoded string to decode.</param>
    /// <returns>The decoded string.</returns>
    public string KeyPasswordGet(string sValue)
    {

        string strR = "";
        char[] charArray = sValue.ToCharArray();
        Array.Reverse(charArray);
        sValue = new string(charArray);

        for (int i = 0; i < sValue.Length; i += 3)
        {
            if (i + 3 <= sValue.Length) // Ensure substring is within bounds
            {
                string strTemp = sValue.Substring(i, 3);
                if (int.TryParse(strTemp, out int intTemp)) // Safe parsing to avoid errors
                {
                    strR += (char)intTemp;
                }
                else
                {
                    // Handle unexpected non-numeric values gracefully
                    return "";
                }
            }
        }

        return strR;

    }
    public Task<List<long>> GetUserIdsByRoleIdAsync(long roleId)
    {
        return _userRepository.Table.Where(x => x.RoleId == roleId).Select(x => x.Id).ToListAsync();
    }
    /// <summary>
    /// Get user role by user identifier
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user role
    /// </returns>
    public async Task<UserRole> GetUserRoleAsync(long userId)
    {
        if (userId == 0)
            throw new ArgumentNullException(nameof(userId));

        var userRoleQuery = from u in _userRepository.Table
                            from ur in _userRoleRepository.Table.InnerJoin(ur => ur.Id == u.RoleId)
                            where u.Id == userId && u.Deleted == false && ur.Deleted == false
                            select ur;
        var query = await userRoleQuery.ToListAsync();
        return query.FirstOrDefault();
    }

    /// <summary>
    /// Insert user
    /// </summary>
    /// <param name="user">User</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user
    /// </returns>
    public async Task InsertUserAsync(User user)
    {
        await _userRepository.InsertAsync(user);
    }

    /// <summary>
    /// Update user
    /// </summary>
    /// <param name="user">User</param>
    /// <returns>
    /// A task that represents the asynchronous operation
    /// The task result contains the user
    /// </returns>
    public async Task UpdateUserAsync(User user)
    {
        await _userRepository.UpdateAsync(user);
    }

    /// <summary>
    /// Delete user
    /// </summary>
    /// <param name="user">User</param>
    /// <returns></returns>
    public async Task DeleteUserAsync(User user)
    {
        await _userRepository.DeleteAsync(user);
    }


    public async Task<IQueryable<UserDataModel>> GetAllUsersAsync(string searchText)
    {
        var query = from d in _userRepository.Table
                    join r in _userRoleRepository.Table on
                    d.RoleId equals r.Id
                    where d.Deleted == false && d.Active == true
                    select new UserDataModel
                    {
                        Id = d.Id,
                        Name = $"{d.FirstName} {d.LastName}",
                        EmailAddress = d.EmailAddress,
                        Token = d.Token,
                        MobileNumber = d.MobileNumber,
                        MultiDeviceLoginEnabled = d.MultiDeviceLoginEnabled,
                        Active = d.Active,
                        RoleId = d.RoleId,
                        Remark = d.Remark,
                        RoleName = r.Name
                    };
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(d => d.Name.Contains(searchText) || d.MobileNumber.Contains(searchText) || d.EmailAddress.Contains(searchText));
        }
        return await Task.FromResult(query);
    }

    public async Task<User> GetUsersByMobileNumberAsync(string mobileNumber)
    {
        return await _userRepository.Table.FirstOrDefaultAsync(c => c.MobileNumber == mobileNumber && c.Deleted == false);
    }
    #endregion
}