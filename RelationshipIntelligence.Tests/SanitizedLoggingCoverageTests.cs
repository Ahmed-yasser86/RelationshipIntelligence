using ContactsManger.Core.Domain.Entities;
using ContactsManger.Core.Domain.Entities.EEnums;
using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using ServiceContracts.DTOs;
using ServiceContracts.DTOs.CopilotDTOs;
using Servicess;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    /// <summary>
    /// Captures the formatted output of every ILogger call. The tests below use
    /// it to prove both that the sanitized (CodeQL log-forging remediation) log
    /// statements execute and that control characters in hostile inputs never
    /// reach the logger.
    /// </summary>
    internal sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoopScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NoopScope : IDisposable
        {
            public static readonly NoopScope Instance = new();
            public void Dispose() { }
        }
    }

    /// <summary>
    /// OrganizationService: invalid-name validation, repository failures and the
    /// sanitized create/delete success log lines.
    /// </summary>
    public class OrganizationServiceSanitizedLoggingTests
    {
        private const string ForgedName = "Acme\r\n[2026-11-25] forged";

        private readonly Mock<CircleRepositryContract> _circles = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly CapturingLogger<OrganizationService> _logger = new();

        private OrganizationService Service() => new(_circles.Object, _unitOfWork.Object, _logger);

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateAsync_InvalidName_ThrowsArgumentException(string name)
        {
            await Assert.ThrowsAsync<ArgumentException>(() => Service().CreateAsync(name));
            _circles.Verify(r => r.AddCircle(It.IsAny<Circle>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_RepositoryFailure_Propagates()
        {
            _circles.Setup(r => r.GetCircleByName(It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("circle store down"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().CreateAsync("Acme"));
        }

        [Fact]
        public async Task CreateAsync_ValidName_LogsSanitizedName()
        {
            _circles.Setup(r => r.GetCircleByName(It.IsAny<string>())).ReturnsAsync((Circle?)null);
            _circles.Setup(r => r.AddCircle(It.IsAny<Circle>())).ReturnsAsync((Circle c) => c);

            var result = await Service().CreateAsync(ForgedName);

            result.Name.Should().Be(ForgedName);
            _logger.Messages.Should().Contain(m => m.Contains("Created organization Acme [2026-11-25] forged"));
            _logger.Messages.Should().NotContain(m => m.Contains("\r") || m.Contains("\n"));
        }

        [Fact]
        public async Task DeleteAsync_RepositoryFailure_Propagates()
        {
            _circles.Setup(r => r.GetCirclesWithMembers())
                .ThrowsAsync(new InvalidOperationException("circle store down"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().DeleteAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task DeleteAsync_EmptyCircle_LogsSanitizedName()
        {
            var id = Guid.NewGuid();
            _circles.Setup(r => r.GetCirclesWithMembers()).ReturnsAsync(new List<Circle>
            {
                new() { CircleId = id, Name = ForgedName, People = new List<Person>() }
            }.AsEnumerable());
            _circles.Setup(r => r.RemoveCircle(It.IsAny<Circle>())).Returns(Task.CompletedTask);

            await Service().DeleteAsync(id);

            _circles.Verify(r => r.RemoveCircle(It.IsAny<Circle>()), Times.Once);
            _logger.Messages.Should().Contain(m => m.Contains("Deleted organization Acme [2026-11-25] forged"));
            _logger.Messages.Should().NotContain(m => m.Contains("\r") || m.Contains("\n"));
        }
    }

    /// <summary>
    /// AiProviderSettingsService: invalid provider/model validation, repository
    /// failures, the sanitized save log line and the unprotect-failure catch.
    /// </summary>
    public class AiProviderSettingsServiceSanitizedLoggingTests
    {
        private readonly Guid _owner = Guid.NewGuid();
        private readonly Mock<AiProviderSettingsRepositoryContract> _settings = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly Mock<IApiKeyProtector> _protector = new();
        private readonly CapturingLogger<AiProviderSettingsService> _logger = new();

        private AiProviderSettingsService Service() => new(
            _settings.Object, _unitOfWork.Object, _currentUser.Object, _protector.Object, _logger);

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SaveAsync_InvalidModel_Throws(string model)
        {
            _currentUser.Setup(u => u.UserId).Returns(_owner);

            await Assert.ThrowsAsync<ArgumentException>(() => Service().SaveAsync(new AiProviderSettingsSaveRequest
            {
                Provider = "Gemini",
                Model = model
            }));
            _settings.Verify(s => s.UpsertAsync(It.IsAny<AiProviderSettings>()), Times.Never);
        }

        [Fact]
        public async Task SaveAsync_InvalidProvider_Throws()
        {
            _currentUser.Setup(u => u.UserId).Returns(_owner);

            await Assert.ThrowsAsync<ArgumentException>(() => Service().SaveAsync(new AiProviderSettingsSaveRequest
            {
                Provider = "Unknown-Provider",
                Model = "model"
            }));
        }

        [Fact]
        public async Task SaveAsync_RepositoryFailure_Propagates()
        {
            _currentUser.Setup(u => u.UserId).Returns(_owner);
            _settings.Setup(s => s.GetAsync(_owner))
                .ThrowsAsync(new InvalidOperationException("settings store down"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().SaveAsync(new AiProviderSettingsSaveRequest
            {
                Provider = "OpenAI",
                Model = "gpt-x"
            }));
        }

        [Fact]
        public async Task SaveAsync_ValidRequest_LogsSanitizedProviderAndModel()
        {
            _currentUser.Setup(u => u.UserId).Returns(_owner);
            _settings.Setup(s => s.GetAsync(_owner)).ReturnsAsync((AiProviderSettings?)null);
            AiProviderSettings? saved = null;
            _settings.Setup(s => s.UpsertAsync(It.IsAny<AiProviderSettings>()))
                .Callback<AiProviderSettings>(s => saved = s)
                .Returns(Task.CompletedTask);

            await Service().SaveAsync(new AiProviderSettingsSaveRequest
            {
                Provider = "gemini",
                Model = "gemini\r\n3.1-flash"
            });

            saved.Should().NotBeNull();
            saved!.Model.Should().Be("gemini\r\n3.1-flash");
            saved.Provider.Should().Be("Gemini");
            _logger.Messages.Should().Contain(m => m.Contains("Saved AI provider settings")
                && m.Contains("Gemini") && m.Contains("gemini 3.1-flash"));
            _logger.Messages.Should().NotContain(m => m.Contains("\r") || m.Contains("\n"));
        }

        [Fact]
        public async Task GetUnprotectedKeyAsync_ProtectorFailure_ReturnsNullAndLogsSanitizedOwnerId()
        {
            _currentUser.Setup(u => u.UserId).Returns(_owner);
            _settings.Setup(s => s.GetAsync(_owner)).ReturnsAsync(new AiProviderSettings
            {
                ApplicationUserId = _owner,
                ProtectedApiKey = Convert.ToBase64String(new byte[] { 1, 2, 3 })
            });
            _protector.Setup(p => p.Unprotect(It.IsAny<byte[]>())).Throws(new CryptographicException("bad key"));

            var result = await Service().GetUnprotectedKeyAsync();

            result.Should().BeNull();
            _logger.Messages.Should().Contain(m => m.Contains("Stored AI provider key could not be unprotected"));
            _logger.Messages.Should().NotContain(m => m.Contains("\r") || m.Contains("\n"));
        }
    }

    /// <summary>
    /// InteractionService: unauthenticated access, repository failures and the
    /// sanitized reminder-completion warning inside the LogAsync catch block.
    /// </summary>
    public class InteractionServiceSanitizedLoggingTests
    {
        private readonly Guid _user = Guid.NewGuid();
        private readonly Mock<InteractionRepositoryContract> _interactions = new();
        private readonly Mock<PersonRepositryContract> _persons = new();
        private readonly Mock<IRelationshipScoringService> _scoring = new();
        private readonly Mock<ICurrentUserService> _currentUser = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly CapturingLogger<InteractionService> _logger = new();

        private InteractionService Service(RelationshipPreferenceRepositoryContract? preferences = null) => new(
            _interactions.Object, _persons.Object, _scoring.Object,
            _currentUser.Object, _unitOfWork.Object, _logger, preferences);

        private static InteractionAddRequest ValidRequest(Guid personId) => new()
        {
            PersonId = personId,
            TimeOfInteraction = DateTime.UtcNow.AddDays(-2),
            InteractionType = EnInteractionType.Email,
            InteractionTitle = "Quarterly plans",
            InteractionDescription = "Follow-up"
        };

        [Fact]
        public async Task LogAsync_ReminderLookupFailure_StillSucceedsAndLogsSanitizedPersonId()
        {
            _currentUser.Setup(u => u.UserId).Returns(_user);
            var personId = Guid.NewGuid();
            _persons.Setup(r => r.GetPersonById(personId)).ReturnsAsync(new Person
            {
                PersonId = personId,
                ApplicationUserId = _user,
                Name = "Layla"
            });
            _interactions.Setup(r => r.AddAsync(It.IsAny<Interaction>()))
                .ReturnsAsync((Interaction i) => i);
            var preferences = new Mock<RelationshipPreferenceRepositoryContract>();
            preferences.Setup(r => r.GetAsync(_user, personId))
                .ThrowsAsync(new InvalidOperationException("preference store down"));

            var result = await Service(preferences.Object).LogAsync(ValidRequest(personId));

            result.Should().NotBeNull();
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _scoring.Verify(s => s.RecomputeForPairAsync(personId), Times.Once);
            _logger.Messages.Should().Contain(m => m.Contains("Reminder completion failed for person"));
            _logger.Messages.Should().NotContain(m => m.Contains("\r") || m.Contains("\n"));
        }

        [Fact]
        public async Task LogAsync_PersonRepositoryFailure_Propagates()
        {
            _currentUser.Setup(u => u.UserId).Returns(_user);
            var personId = Guid.NewGuid();
            _persons.Setup(r => r.GetPersonById(personId))
                .ThrowsAsync(new InvalidOperationException("person store down"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().LogAsync(ValidRequest(personId)));
            _interactions.Verify(r => r.AddAsync(It.IsAny<Interaction>()), Times.Never);
        }

        [Fact]
        public async Task ImportCsvAsync_Unauthenticated_ThrowsUnauthorizedAccessException()
        {
            _currentUser.Setup(u => u.UserId).Returns((Guid?)null);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service()
                .ImportCsvAsync(Guid.NewGuid(), "date,type,title\n2026-01-01,Email,x"));
            _persons.Verify(r => r.GetPersonById(It.IsAny<Guid?>()), Times.Never);
        }
    }

    /// <summary>
    /// PersonGetterService: repository failures, missing persons and invalid
    /// pagination arguments across the sanitized log statements.
    /// </summary>
    public class PersonGetterServiceSanitizedLoggingTests
    {
        private readonly Mock<PersonRepositryContract> _persons = new();
        private readonly CapturingLogger<PersonGetterService> _logger = new();

        private PersonGetterService Service() => new(_persons.Object, _logger);

        [Fact]
        public async Task GetAllPersons_RepositoryFailure_PropagatesAndLogsError()
        {
            _persons.Setup(r => r.GetAllPersons())
                .ThrowsAsync(new InvalidOperationException("person store down"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().GetAllPersons());
            _logger.Messages.Should().Contain(m => m.Contains("Error occurred in GetAllPersons"));
        }

        [Fact]
        public async Task GetPersonByPersonId_RepositoryFailure_PropagatesAndLogsSanitizedId()
        {
            var id = Guid.NewGuid();
            _persons.Setup(r => r.GetPersonById(id))
                .ThrowsAsync(new InvalidOperationException("person store down"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().GetPersonByPersonId(id));
            _logger.Messages.Should().Contain(m => m.Contains("Error occurred in GetPersonByPersonId"));
            _logger.Messages.Should().Contain(m => m.Contains(id.ToString()));
        }

        [Fact]
        public async Task GetPersonByPersonId_UnknownId_ReturnsNullAndLogsSanitizedId()
        {
            var id = Guid.NewGuid();
            _persons.Setup(r => r.GetPersonById(id)).ReturnsAsync((Person?)null);

            var result = await Service().GetPersonByPersonId(id);

            result.Should().BeNull();
            _logger.Messages.Should().Contain(m => m.Contains("No person found with ID"));
        }

        [Fact]
        public async Task GetPersonsViewBatched_InvalidPageArguments_ClampsAndLogsSanitizedValues()
        {
            _persons.Setup(r => r.GetPersonsPaged(1, 1))
                .ReturnsAsync((new List<Person>(), 0));

            var result = await Service().GetPersonsViewBatched(0, -5);

            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(1);
            _persons.Verify(r => r.GetPersonsPaged(1, 1), Times.Once);
            _logger.Messages.Should().Contain(m => m.Contains("invalid PageNumber"));
            _logger.Messages.Should().Contain(m => m.Contains("invalid PageSize"));
            _logger.Messages.Should().NotContain(m => m.Contains("\r") || m.Contains("\n"));
        }
    }

    /// <summary>
    /// PersonUpdaterService: non-existent person, validation/concurrency catch
    /// paths, forced repository failures and sanitized field-change logging.
    /// </summary>
    public class PersonUpdaterServiceSanitizedLoggingTests
    {
        private readonly Mock<PersonRepositryContract> _persons = new();
        private readonly Mock<CircleRepositryContract> _circles = new();
        private readonly Mock<ContactItemRoleRepositryContract> _roles = new();
        private readonly Mock<ConnectionChannelRepositryContract> _channels = new();
        private readonly Mock<UserDefinedTagsRepositryContract> _tags = new();
        private readonly Mock<SystemStatusTagRepositryContract> _statusTags = new();
        private readonly Mock<SocialMediaAccountRepositryContract> _social = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly CapturingLogger<PersonUpdaterService> _logger = new();

        private PersonUpdaterService Service() => new(
            _persons.Object, _circles.Object, _roles.Object, _channels.Object,
            _tags.Object, _statusTags.Object, _logger, _social.Object, _unitOfWork.Object);

        private static PersonUpdateRequest ValidRequest(Guid id, string? name = null) => new()
        {
            PersonId = id,
            Name = name ?? "Layla",
            email = "layla@example.com",
            phone = "123456789",
            Organizations = null,
            CurrentRoles = null,
            ConnectionChannels = null,
            UserDefinedTags = null,
            SystemStatusTags = null,
            SocialMediaAccounts = null
        };

        [Fact]
        public async Task UpdatePerson_NonExistentPerson_ThrowsAndLogsSanitizedId()
        {
            var id = Guid.NewGuid();
            _persons.Setup(r => r.GetPersonById(id)).ReturnsAsync((Person?)null);

            await Assert.ThrowsAsync<ArgumentException>(() => Service().UpdatePerson(ValidRequest(id)));

            _logger.Messages.Should().Contain(m => m.Contains("Attempted to update non-existent person"));
            _logger.Messages.Should().Contain(m => m.Contains("Argument error in UpdatePerson"));
            _logger.Messages.Should().Contain(m => m.Contains(id.ToString()));
        }

        [Fact]
        public async Task UpdatePerson_ValidationExceptionFromRepository_PropagatesAndLogsSanitizedRequest()
        {
            var id = Guid.NewGuid();
            _persons.Setup(r => r.GetPersonById(id))
                .ThrowsAsync(new ValidationException("stale person row"));

            await Assert.ThrowsAsync<ValidationException>(() => Service().UpdatePerson(ValidRequest(id)));

            _logger.Messages.Should().Contain(m => m.Contains("Validation error in UpdatePerson"));
        }

        [Fact]
        public async Task UpdatePerson_UnexpectedRepositoryFailure_PropagatesAndLogsSanitizedRequest()
        {
            var id = Guid.NewGuid();
            _persons.Setup(r => r.GetPersonById(id))
                .ThrowsAsync(new InvalidOperationException("person store down"));

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().UpdatePerson(ValidRequest(id)));

            _logger.Messages.Should().Contain(m => m.Contains("Unexpected error occurred in UpdatePerson"));
        }

        [Fact]
        public async Task UpdatePerson_ValidRequest_LogsSanitizedFieldChanges()
        {
            var id = Guid.NewGuid();
            _persons.Setup(r => r.GetPersonById(id)).ReturnsAsync(new Person
            {
                PersonId = id,
                Name = "Old\r\nName",
                email = "layla@example.com",
                ApplicationUserId = Guid.NewGuid()
            });

            var result = await Service().UpdatePerson(ValidRequest(id, "New\r\nName"));

            result.Should().NotBeNull();
            _logger.Messages.Should().Contain(m => m.Contains("Updating Name from 'Old Name' to 'New Name'"));
            _logger.Messages.Should().Contain(m => m.Contains("Successfully updated person"));
            _logger.Messages.Should().NotContain(m => m.Contains("\r") || m.Contains("\n"));
        }
    }
}
