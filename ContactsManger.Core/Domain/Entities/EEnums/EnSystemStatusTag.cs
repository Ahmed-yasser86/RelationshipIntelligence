using System;

namespace ContactsManger.Core.Domain.Entities.EEnums
{
    public enum EnSystemStatusTag
    {
        /// <summary>
        /// For contacts that have high priority,
        /// need to be addressed immediately,
        /// and trigger real-time notifications and alerts.
        /// </summary>
        HighPriority = 1,

        /// <summary>
        /// For urgent follow up.
        /// </summary>
        Urgent = 2,

        /// <summary>
        /// For contacts that connected with you and you 
        /// have not yet replied.
        /// </summary>
        HaveNotReplied = 3,

        /// <summary>
        /// For contacts that need to be answered or responded to.
        /// </summary>
        ShouldReply = 4,

        /// <summary>
        /// For users needing standard follow up with this contact.
        /// </summary>
        FollowUp = 5,

        /// <summary>
        /// For contacts that have moderate priority.
        /// </summary>
        ModeratePriority = 6,

        /// <summary>
        /// For contacts that have been reached out to recently.
        /// </summary>
        ContactedRecently = 7,

        /// <summary>
        /// For contacts that have already been replied to or responded to.
        /// </summary>
        AlreadyReplied = 8,

        /// <summary>
        /// For contacts that have low priority
        /// and can be addressed later.
        /// </summary>
        LowPriority = 9,

        /// <summary>
        /// For contacts that you highlight to ignore.
        /// </summary>
        Ignored = 10
    }
}