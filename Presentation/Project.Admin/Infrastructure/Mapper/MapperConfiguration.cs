using AutoMapper;

using Project.Admin.Models.Catalog;

using Project.Admin.Models.Users;
using Project.Core.Domain.Catalog;
using Project.Core.Domain.Users;
using Project.Core.Infrastructure.Mapper;
using Project.Services.Users.EntityModel;
namespace Project.Admin.Infrastructure.Mapper;

/// <summary>
/// AutoMapper configuration for admin area models
/// </summary>
public partial class MapperConfiguration : Profile, IOrderedMapperProfile
{
    #region Ctor

    public MapperConfiguration()
    {
        //create specific maps
        CreateUserMaps();
        CreateCatalogMaps();

    }

    #endregion

    #region Utilities

    protected virtual void CreateUserMaps()
    {
        CreateMap<User, UserModel>();

        CreateMap<User, UserAuthModel>();

        CreateMap<UserEntityModel, UserModel>();

        CreateMap<UserModel, User>()
            .ForMember(entity => entity.Deleted, options => options.Ignore())
            .ForMember(entity => entity.CreatedOnUtc, options => options.Ignore())
            .ForMember(entity => entity.UpdatedOnUtc, options => options.Ignore());
    }

    protected virtual void CreateCatalogMaps()
    {
        //user roles
        CreateMap<UserRole, UserRoleModel>();
        CreateMap<UserRoleModel, UserRole>()
            .ForMember(entity => entity.Deleted, options => options.Ignore())
            .ForMember(entity => entity.SystemName, options => options.Ignore())
            .ForMember(entity => entity.CreatedOnUtc, options => options.Ignore())
            .ForMember(entity => entity.UpdatedOnUtc, options => options.Ignore());



        //departments
        CreateMap<Department, DepartmentModel>();
        CreateMap<DepartmentModel, Department>()
            .ForMember(entity => entity.Deleted, options => options.Ignore())
            .ForMember(entity => entity.CreatedOnUtc, options => options.Ignore())
            .ForMember(entity => entity.UpdatedOnUtc, options => options.Ignore());
    }





    #endregion

    #region Properties

    /// <summary>
    /// Order of this mapper implementation
    /// </summary>
    public int Order => 1;

    #endregion
}
