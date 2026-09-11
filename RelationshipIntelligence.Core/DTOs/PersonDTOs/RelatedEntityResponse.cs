using ContactsManger.Core.Domain.Entities;
using ContactsManger.Core.Domain.Entities.EEnums;
using System;

namespace ServiceContracts.DTOs
{
    public class CircleResponse
    {
        public Guid CircleId { get; set; }
        public string Name { get; set; }
    }

    public class ContactItemRoleResponse
    {
        public Guid ContactsRoleId { get; set; }
        public string Role { get; set; }
    }

    public class ConnectionChannelResponse
    {
        public Guid ConnectionChannelId { get; set; }
        public string ConnectionChannelName { get; set; }
    }

    public class SystemStatusTagResponse
    {
        public EnSystemStatusTag StatusTagId { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }

    public class UserDefinedTagsResponse
    {
        public Guid TagId { get; set; }
        public string TagName { get; set; }
    }

    public class SocialMediaAccountResponse
    {
        public Guid SocialMediaAccountId { get; set; }
        public string? Platform { get; set; }
        public string Url { get; set; }
    }

    public class NoteResponse
    {
        public Guid NoteId { get; set; }
        public EnNoteType NoteType { get; set; }
        public string Content { get; set; }
    }

    public class InteractionResponse
    {
        public Guid InteractionId { get; set; }
        public Guid PersonId { get; set; }
        public EnInteractionType InteractionType { get; set; }
        public string InteractionTitle { get; set; }
        public string? InteractionDescription { get; set; }
        public DateTime TimeOfInteraction { get; set; }
    }

    public static class RelatedEntityResponseExtensions
    {
        public static CircleResponse ConvertToDto(this Circle circle)
        {
            if (circle == null) return null;
            return new CircleResponse { CircleId = circle.CircleId, Name = circle.Name };
        }

        public static ContactItemRoleResponse ConvertToDto(this ContactItemRole role)
        {
            if (role == null) return null;
            return new ContactItemRoleResponse { ContactsRoleId = role.ContactsRoleId, Role = role.Role };
        }

        public static ConnectionChannelResponse ConvertToDto(this ConnectionChannel channel)
        {
            if (channel == null) return null;
            return new ConnectionChannelResponse
            {
                ConnectionChannelId = channel.ConnectionChannelId,
                ConnectionChannelName = channel.ConnectionChannelName
            };
        }

        public static SystemStatusTagResponse ConvertToDto(this SystemStatusTag tag)
        {
            if (tag == null) return null;
            return new SystemStatusTagResponse
            {
                StatusTagId = tag.StatusTagId,
                Name = tag.Name,
                Description = tag.Description
            };
        }

        public static UserDefinedTagsResponse ConvertToDto(this UserDefinedTags tag)
        {
            if (tag == null) return null;
            return new UserDefinedTagsResponse { TagId = tag.TagId, TagName = tag.TagName };
        }

        public static SocialMediaAccountResponse ConvertToDto(this SocialMediaAccount account)
        {
            if (account == null) return null;
            return new SocialMediaAccountResponse
            {
                SocialMediaAccountId = account.SocialMediaAccountId,
                Platform = account.Platform,
                Url = account.Url
            };
        }

        public static NoteResponse ConvertToDto(this Note note)
        {
            if (note == null) return null;
            return new NoteResponse
            {
                NoteId = note.NoteId,
                NoteType = note.NoteType,
                Content = note.Content
            };
        }

        public static InteractionResponse ConvertToDto(this Interaction interaction)
        {
            if (interaction == null) return null;
            return new InteractionResponse
            {
                InteractionId = interaction.InteractionId,
                PersonId = interaction.PersonId,
                InteractionType = interaction.InteractionType,
                InteractionTitle = interaction.InteractionTitle,
                InteractionDescription = interaction.InteractionDescription,
                TimeOfInteraction = interaction.TimeOfInteraction
            };
        }

        public static List<InteractionResponse> ConvertToDtos(this IEnumerable<Interaction> interactions)
        {
            return interactions.Select(i => i.ConvertToDto()).ToList();
        }
    }
}