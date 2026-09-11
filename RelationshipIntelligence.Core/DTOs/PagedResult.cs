using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace ServiceContracts.DTOs
{
    /// <summary>
    /// Generic paged-result envelope. TotalCount lets the frontend compute
    /// total pages / know when to stop showing "load more" without a
    /// separate count request.
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public bool HasMore => (long)PageNumber * PageSize < TotalCount;
    }
}

