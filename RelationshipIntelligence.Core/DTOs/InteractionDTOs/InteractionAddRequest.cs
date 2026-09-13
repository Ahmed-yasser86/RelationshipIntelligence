using ContactsManger.Core.CustomValidations;
using ContactsManger.Core.Domain.Entities;
using ContactsManger.Core.Domain.Entities.EEnums;
using System;
using System.ComponentModel.DataAnnotations;

namespace ServiceContracts.DTOs
{
    public class InteractionAddRequest
    {
        [Required]
        public Guid? PersonId { get; set; }

        [Required]
        public DateTime? TimeOfInteraction { get; set; }

        [Required]
        [EnumRange(typeof(EnInteractionType))]
        public EnInteractionType? InteractionType { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
        public string? InteractionTitle { get; set; }

        [StringLength(2000, ErrorMessage = "Description is too long")]
        public string? InteractionDescription { get; set; }

        public Guid? SourceMeetingId { get; set; }

        public Interaction ToInteraction()
        {
            return new Interaction
            {
                InteractionId = Guid.NewGuid(),
                PersonId = (Guid)PersonId!,
                TimeOfInteraction = ((DateTime)TimeOfInteraction!).ToUniversalTime(),
                InteractionType = (EnInteractionType)InteractionType!,
                InteractionTitle = InteractionTitle!,
                InteractionDescription = InteractionDescription,
                SourceMeetingId = SourceMeetingId
            };
        }
    }
}
