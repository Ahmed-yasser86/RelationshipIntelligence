using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactsManger.Core.Domain.Entities.EEnums
{
    public enum EnSystemStatusTag
    {
        /// <summary>
        /// for users need follow up with this contact
        /// </summary>
        FollowUp = 1,
        /// <summary>
        /// for urgent follow up
        /// </summary>
        Urgent = 2,
        /// <summary>
        /// for contacts that have been reached out to recently
        /// </summary>
        ContactedRecently = 3,
        /// <summary>
        /// for contacts that need to be answered or responded to
        /// </summary>
        ShouldReply = 4,
        /// <summary>
        /// for contacts that have already been replied to or responded to
        /// </summary>
        AlreadyReplied = 5,
        /// <summary>
        /// for contacts that connected with you and you 
        /// have not yet replied 
        /// </summary>
        HaveNotReplied = 6,
        /// <summary>
        /// for contacts that you highlite to ignore
        /// </summary>
        Ignored = 7,
        /// <summary>
        /// for contacts that have high priority
        /// and need to be addressed immediately
        /// and get reltime notifications and alerts about them
        /// </summary>
        HighPriority = 8,
        /// <summary>
        /// for contacts that have low priority
        /// and can be addressed later
        /// </summary>
        LowPriority = 9,

        /// <summary>
        /// for contacts that have moderate priority
        /// </summary>
        Modratepriority = 10,

    }
}
