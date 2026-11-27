using Project.Core;

namespace Project.Admin.Models.Common;

/// <summary>Response shape for admin list endpoints: one page of items plus paging info.</summary>
public class PagedResultModel<T>
{
    public IList<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }

    public static PagedResultModel<T> From(IPagedList<T> list) => new()
    {
        Items = list,
        TotalCount = list.TotalCount,
        PageIndex = list.PageIndex,
        PageSize = list.PageSize,
        TotalPages = list.TotalPages,
        HasNextPage = list.HasNextPage,
        HasPreviousPage = list.HasPreviousPage
    };
}
