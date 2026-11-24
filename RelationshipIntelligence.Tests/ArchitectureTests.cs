using FluentAssertions;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace CRUDTests
{
    /// <summary>
    /// Codifies the V2 dependency direction: Core must never reference the AI
    /// project, and deterministic scoring must not depend on AI/meeting-brief/draft types.
    /// </summary>
    public class ArchitectureTests
    {
        private static Assembly CoreAssembly() =>
            typeof(ServiceContracts.ICopilotService).Assembly;

        [Fact]
        public void Core_DoesNotReferenceAiProject()
        {
            var referenced = CoreAssembly().GetReferencedAssemblies().Select(a => a.Name);
            referenced.Should().NotContain(n => n != null && n.Contains("RelationshipIntelligence.AI"));
        }

        [Fact]
        public void Scoring_DoesNotReferenceBriefOrDraftTypes()
        {
            var scoringTypes = CoreAssembly()
                .GetTypes()
                .Where(t => t.FullName != null && t.FullName.Contains("RelationshipScoringService"));
            foreach (var type in scoringTypes)
            {
                var methodBodies = type.GetMethods().SelectMany(m => m.GetParameters()).Select(p => p.ParameterType);
                var names = methodBodies.Select(t => t.FullName ?? string.Empty);
                names.Should().NotContain(n =>
                    n.Contains("MeetingBrief") || n.Contains("CommunicationDraft") || n.Contains("Copilot"),
                    $"because {type.Name} must remain deterministic application logic");
            }
        }

        [Fact]
        public void CopilotContract_LivesInCore_NotInAi()
        {
            typeof(ServiceContracts.ICopilotService).Assembly.GetName().Name
                .Should().Contain("RelationshipIntelligence.Core");
        }

        [Fact]
        public void Core_ReferencesOnlyPermittedFrameworkAssemblies()
        {
            var forbidden = new[]
            {
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Serilog",
                "SerilogTimings",
                "Microsoft.Extensions.Configuration",
                "Microsoft.Extensions.Identity"
            };

            var referenced = CoreAssembly().GetReferencedAssemblies()
                .Select(a => a.Name ?? string.Empty)
                .ToList();

            foreach (var prefix in forbidden)
            {
                referenced.Should().NotContain(n => n.StartsWith(prefix, StringComparison.Ordinal),
                    $"because Core must not depend on implementation framework '{prefix}'");
            }
        }

        [Fact]
        public void Core_DoesNotContainInfrastructureOrWebImplementations()
        {
            var names = CoreAssembly().GetTypes()
                .Where(t => t.FullName != null)
                .Select(t => t.FullName!)
                .ToList();

            names.Should().NotContain(n => n.EndsWith(".JwtServices", StringComparison.Ordinal));
            names.Should().NotContain(n => n.EndsWith(".CurrentUserService", StringComparison.Ordinal));
            names.Should().NotContain(n => n.EndsWith(".FileEmailSender", StringComparison.Ordinal));
            names.Should().NotContain(n => n.EndsWith(".ApiKeyProtector", StringComparison.Ordinal));
        }

        [Fact]
        public void CoreDtos_HaveNoMvcAttributes()
        {
            var dtos = CoreAssembly().GetTypes()
                .Where(t => t.FullName != null && t.FullName.Contains(".DTOs", StringComparison.Ordinal))
                .ToList();

            dtos.Should().NotBeEmpty();

            foreach (var dto in dtos)
            {
                var mvcAttributes = dto.GetCustomAttributesData()
                    .Where(a => a.AttributeType.FullName != null &&
                                a.AttributeType.FullName.StartsWith("Microsoft.AspNetCore.Mvc", StringComparison.Ordinal))
                    .ToList();
                mvcAttributes.Should().BeEmpty($"because {dto.Name} is a framework-neutral Core DTO");
            }
        }
    }
}
