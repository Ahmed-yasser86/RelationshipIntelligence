using AutoFixture;
using AutoFixture.Kernel;
using ContactsManger.Core.Domain.Entities;
using ContactsManger.Core.DTOs.PersonDTOs;
using Entities;
using EntityFrameworkCoreMock;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using ServiceContracts.DTOs;
using ServiceContracts.DTOs.Enums;
using Servicess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class PersonServicesTest
    {
        private readonly IPersonGetterService _personGetterService;
        private readonly IPersonAdderService _personAdderService;
        private readonly IPersonQuickAdderService _personQuickAdderService;
        private readonly IPersonUpdaterService _personUpdaterService;
        private readonly IPersonDeleterService _personDeleterService;
        private readonly IPersonSearcherService _personSearcherService;
        private readonly IPersonSorterService _personSorterService;
        private readonly IFixture _fixture;
        private readonly IUnitOfWork _unitOfWork;
        private readonly Mock<PersonRepositryContract> _personRepositryContractMoq;
        private readonly PersonRepositryContract _personRepositryContract;

        private readonly Mock<CircleRepositryContract> _circleRepositryContractMoq;
        private readonly CircleRepositryContract _circleRepositryContract;

        private readonly Mock<ContactItemRoleRepositryContract> _contactItemRoleRepositryContractMoq;
        private readonly ContactItemRoleRepositryContract _contactItemRoleRepositryContract;

        private readonly Mock<ConnectionChannelRepositryContract> _connectionChannelRepositryContractMoq;
        private readonly ConnectionChannelRepositryContract _connectionChannelRepositryContract;

        private readonly Mock<UserDefinedTagsRepositryContract> _userDefinedTagsRepositryContractMoq;
        private readonly UserDefinedTagsRepositryContract _userDefinedTagsRepositryContract;

        private readonly Mock<SystemStatusTagRepositryContract> _systemStatusTagRepositryContractMoq;
        private readonly SystemStatusTagRepositryContract _systemStatusTagRepositryContract;

        private readonly Mock<ICurrentUserService> _currentUserServiceMoq;
        private readonly ICurrentUserService _currentUserService;
        private readonly Guid _testUserId;
        private readonly Mock<IUnitOfWork> _unitOfWorkMoq;

        private readonly Mock<SocialMediaAccountRepositryContract> _SocialMediaAccountRepositryContractMoq;
        private readonly SocialMediaAccountRepositryContract _socialMediaAccountRepositryContract;

        public PersonServicesTest()
        {
            _fixture = new Fixture();

            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
                .ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            List<Person> persons = new List<Person>();
            List<Country> countries = new List<Country>();
            DbContextMock<AppDBContext> dbContextMock = new DbContextMock<AppDBContext>(new DbContextOptionsBuilder<AppDBContext>().Options);

            _SocialMediaAccountRepositryContractMoq = new Mock<SocialMediaAccountRepositryContract>();
            _socialMediaAccountRepositryContract =  _SocialMediaAccountRepositryContractMoq.Object;
            _unitOfWorkMoq = new Mock<IUnitOfWork>();
            _unitOfWork = _unitOfWorkMoq.Object; 

            _personRepositryContractMoq = new Mock<PersonRepositryContract>();
            _personRepositryContract = _personRepositryContractMoq.Object;

            _circleRepositryContractMoq = new Mock<CircleRepositryContract>();
            _circleRepositryContract = _circleRepositryContractMoq.Object;

            _contactItemRoleRepositryContractMoq = new Mock<ContactItemRoleRepositryContract>();
            _contactItemRoleRepositryContract = _contactItemRoleRepositryContractMoq.Object;

            _connectionChannelRepositryContractMoq = new Mock<ConnectionChannelRepositryContract>();
            _connectionChannelRepositryContract = _connectionChannelRepositryContractMoq.Object;

            _userDefinedTagsRepositryContractMoq = new Mock<UserDefinedTagsRepositryContract>();
            _userDefinedTagsRepositryContract = _userDefinedTagsRepositryContractMoq.Object;

            _systemStatusTagRepositryContractMoq = new Mock<SystemStatusTagRepositryContract>();
            _systemStatusTagRepositryContract = _systemStatusTagRepositryContractMoq.Object;

            // Default: every test runs as an authenticated user unless a specific
            // test overrides this mock to simulate an unauthenticated context.
            _testUserId = Guid.NewGuid();
            _currentUserServiceMoq = new Mock<ICurrentUserService>();
            _currentUserServiceMoq.Setup(s => s.UserId).Returns(_testUserId);
            _currentUserService = _currentUserServiceMoq.Object;

            dbContextMock.CreateDbSetMock(temp => temp.Persons, persons);
            dbContextMock.CreateDbSetMock(temp => temp.Countries, countries);

            var loggerGetterMock = new Mock<ILogger<PersonGetterService>>();
            var loggerAdderMock = new Mock<ILogger<PersonAdderService>>();
            var loggerQuickAdderMock = new Mock<ILogger<PersonQuickAdderService>>();
            var loggerUpdaterMock = new Mock<ILogger<PersonUpdaterService>>();
            var loggerDeleterMock = new Mock<ILogger<PersonDeleterService>>();
            var loggerSearcherMock = new Mock<ILogger<PersonSearcherService>>();
            var loggerSorterMock = new Mock<ILogger<PersonSorterService>>();

            _personGetterService = new PersonGetterService(_personRepositryContract, loggerGetterMock.Object);

            // PersonAdderService's real constructor takes six repository
            // contracts (Person, Circle, ContactItemRole, ConnectionChannel,
            // UserDefinedTags, SystemStatusTag) + ICurrentUserService (for
            // stamping ApplicationUserId from the authenticated user, never
            // from client request data) + logger. All get-or-create lookups
            // (Organizations -> Circle, ConnectionChannels -> by name,
            // UserDefinedTags -> by name, SystemStatusTags -> by enum) happen
            // in this service, not in the Repository.
            _personAdderService = new PersonAdderService(
                _personRepositryContract,
                _circleRepositryContract,
                _contactItemRoleRepositryContract,
                _connectionChannelRepositryContract,
                _userDefinedTagsRepositryContract,
                _systemStatusTagRepositryContract,
                _currentUserService, _unitOfWork,
                loggerAdderMock.Object);

            // PersonQuickAdderService only depends on Person + Circle repos
            // (Quick Add only resolves Organizations -> Circle; CurrentRoles
            // are created directly as new ContactItemRole rows, no repo lookup),
            // plus ICurrentUserService for ApplicationUserId stamping.
            _personQuickAdderService = new PersonQuickAdderService(
                _personRepositryContract,
                _circleRepositryContract,
                _currentUserService, _unitOfWork,
                loggerQuickAdderMock.Object);



            // PersonUpdaterService has the same six-dependency shape as Adder.
            _personUpdaterService = new PersonUpdaterService(
                _personRepositryContract,
                _circleRepositryContract,
                _contactItemRoleRepositryContract,
                _connectionChannelRepositryContract,
                _userDefinedTagsRepositryContract,
                _systemStatusTagRepositryContract,
                loggerUpdaterMock.Object, _socialMediaAccountRepositryContract, _unitOfWork);

            _personDeleterService = new PersonDeleterService(_unitOfWork, _personRepositryContract, loggerDeleterMock.Object);
            _personSearcherService = new PersonSearcherService(_personRepositryContract, loggerSearcherMock.Object);
            _personSorterService = new PersonSorterService(_personRepositryContract, loggerSorterMock.Object);
        }

        #region AddPerson Tests

        [Fact]
        public async Task AddPerson_null()
        {
            // Arrange
            PersonAddRequest? personAddRequest = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _personAdderService.AddPerson(personAddRequest));
        }

        [Fact]
        public async Task AddPerson_nullPersonName()
        {
            // Arrange
            PersonAddRequest? personAddRequest = _fixture.Build<PersonAddRequest>()
                .With(p => p.Name, (string?)null)
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .With(p => p.Gender, GenderOptions.Male)
                .Create();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await _personAdderService.AddPerson(personAddRequest));
        }

        [Fact]
        public async Task AddPerson_ProperPersonDetails_PersonAddSuccessfully()
        {
            // Arrange
            PersonAddRequest? personAddRequest = _fixture.Build<PersonAddRequest>()
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .With(p => p.Gender, GenderOptions.Male)
                .With(p => p.Organizations, (List<string>?)null)
                .With(p => p.CurrentRoles, (List<string>?)null)
                .With(p => p.ConnectionChannels, (List<string>?)null)
                .With(p => p.UserDefinedTags, (List<string>?)null)
                .With(p => p.SystemStatusTags, (List<ContactsManger.Core.Domain.Entities.EEnums.EnSystemStatusTag>?)null)
                .With(p => p.SocialMediaAccounts, (List<SocialMediaAccountAddRequest>?)null)
                .Create();

            _personRepositryContractMoq
                .Setup(repo => repo.AddPerson(It.IsAny<Person>()))
                .ReturnsAsync((Person p) => p);

            // Act
            var personResponse_actual = await _personAdderService.AddPerson(personAddRequest);

            // Assert
            personResponse_actual.PersonId.Should().NotBe(Guid.Empty);
            personResponse_actual.Name.Should().Be(personAddRequest.Name);
            personResponse_actual.email.Should().Be(personAddRequest.email);
        }

        /// <summary>
        /// Covers the full-profile scalar fields on PersonAddRequest (ContextMemory,
        /// ProfileImagePath, Origin, LinkedInProfile, OtherInformation). Mapped
        /// straight through ToPerson(), so the assertion is that they survive the
        /// round trip into the response.
        /// </summary>
        [Fact]
        public async Task AddPerson_WithProfileFields_PersistsScalarFieldsCorrectly()
        {
            // Arrange
            PersonAddRequest? personAddRequest = _fixture.Build<PersonAddRequest>()
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .With(p => p.Gender, GenderOptions.Male)
                .With(p => p.ContextMemory, "Met at a conference, follow up about the API project")
                .With(p => p.ProfileImagePath, "/images/profile123.png")
                .With(p => p.Origin, "Met at PyCon 2026")
                .With(p => p.LinkedInProfile, "https://linkedin.com/in/testperson")
                .With(p => p.OtherInformation, "Prefers async communication")
                .With(p => p.Organizations, (List<string>?)null)
                .With(p => p.CurrentRoles, (List<string>?)null)
                .With(p => p.ConnectionChannels, (List<string>?)null)
                .With(p => p.UserDefinedTags, (List<string>?)null)
                .With(p => p.SystemStatusTags, (List<ContactsManger.Core.Domain.Entities.EEnums.EnSystemStatusTag>?)null)
                .With(p => p.SocialMediaAccounts, (List<SocialMediaAccountAddRequest>?)null)
                .Create();

            _personRepositryContractMoq
                .Setup(repo => repo.AddPerson(It.IsAny<Person>()))
                .ReturnsAsync((Person p) => p);

            // Act
            var result = await _personAdderService.AddPerson(personAddRequest);

            // Assert
            result.ContextMemory.Should().Be(personAddRequest.ContextMemory);
            result.ProfileImagePath.Should().Be(personAddRequest.ProfileImagePath);
            result.Origin.Should().Be(personAddRequest.Origin);
            result.LinkedInProfile.Should().Be(personAddRequest.LinkedInProfile);
            result.OtherInformation.Should().Be(personAddRequest.OtherInformation);
        }

        /// <summary>
        /// AddPerson resolves Organizations -> Circle via get-or-create
        /// (CircleRepositryContract.GetCircleByName). No existing Circle named
        /// "Acme Inc" -> null, service creates a brand-new one.
        /// </summary>
        [Fact]
        public async Task AddPerson_WithNewOrganization_CreatesNewCircle()
        {
            // Arrange
            PersonAddRequest? personAddRequest = _fixture.Build<PersonAddRequest>()
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .With(p => p.Gender, GenderOptions.Male)
                .With(p => p.Organizations, new List<string> { "Acme Inc" })
                .With(p => p.CurrentRoles, (List<string>?)null)
                .With(p => p.ConnectionChannels, (List<string>?)null)
                .With(p => p.UserDefinedTags, (List<string>?)null)
                .With(p => p.SystemStatusTags, (List<ContactsManger.Core.Domain.Entities.EEnums.EnSystemStatusTag>?)null)
                .With(p => p.SocialMediaAccounts, (List<SocialMediaAccountAddRequest>?)null)
                .Create();

            // AddPerson now resolves Organizations via a single batched call
            // (GetCirclesByNames) instead of one GetCircleByName call per name.
            _circleRepositryContractMoq
                .Setup(repo => repo.GetCirclesByNames(It.Is<IEnumerable<string>>(names => names.Contains("Acme Inc"))))
                .ReturnsAsync(new List<Circle>());

            Person? captured = null;
            _personRepositryContractMoq
                .Setup(repo => repo.AddPerson(It.IsAny<Person>()))
                .Callback<Person>(p => captured = p)
                .ReturnsAsync((Person p) => p);

            // Act
            await _personAdderService.AddPerson(personAddRequest);

            // Assert
            captured.Should().NotBeNull();
            captured!.Circles.Should().ContainSingle(c => c.Name == "Acme Inc");
            _circleRepositryContractMoq.Verify(
                repo => repo.GetCirclesByNames(It.Is<IEnumerable<string>>(names => names.Contains("Acme Inc"))),
                Times.Once);
        }

        /// <summary>
        /// AddPerson resolves ConnectionChannels via get-or-create against
        /// ConnectionChannelRepositryContract.GetConnectionChannelByName.
        /// </summary>
        [Fact]
        public async Task AddPerson_WithNewConnectionChannel_CreatesNewChannel()
        {
            // Arrange
            PersonAddRequest? personAddRequest = _fixture.Build<PersonAddRequest>()
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .With(p => p.Gender, GenderOptions.Male)
                .With(p => p.Organizations, (List<string>?)null)
                .With(p => p.CurrentRoles, (List<string>?)null)
                .With(p => p.ConnectionChannels, new List<string> { "WhatsApp" })
                .With(p => p.UserDefinedTags, (List<string>?)null)
                .With(p => p.SystemStatusTags, (List<ContactsManger.Core.Domain.Entities.EEnums.EnSystemStatusTag>?)null)
                .With(p => p.SocialMediaAccounts, (List<SocialMediaAccountAddRequest>?)null)
                .Create();

            // Batched lookup: GetConnectionChannelsByNames replaces the old
            // per-name GetConnectionChannelByName call.
            _connectionChannelRepositryContractMoq
                .Setup(repo => repo.GetConnectionChannelsByNames(It.Is<IEnumerable<string>>(names => names.Contains("WhatsApp"))))
                .ReturnsAsync(new List<ConnectionChannel>());

            Person? captured = null;
            _personRepositryContractMoq
                .Setup(repo => repo.AddPerson(It.IsAny<Person>()))
                .Callback<Person>(p => captured = p)
                .ReturnsAsync((Person p) => p);

            // Act
            await _personAdderService.AddPerson(personAddRequest);

            // Assert
            captured.Should().NotBeNull();
            captured!.ConnectionChannels.Should().ContainSingle(c => c.ConnectionChannelName == "WhatsApp");
        }

        /// <summary>
        /// SystemStatusTag rows are fixed reference/seed data keyed by enum value.
        /// If the requested enum has no seeded row, AddPerson must skip it silently
        /// rather than fabricate reference data it doesn't own.
        /// </summary>
        [Fact]
        public async Task AddPerson_WithMissingSystemStatusTagSeed_SkipsTagWithoutThrowing()
        {
            // Arrange
            var requestedEnum = ContactsManger.Core.Domain.Entities.EEnums.EnSystemStatusTag.ModeratePriority;

            PersonAddRequest? personAddRequest = _fixture.Build<PersonAddRequest>()
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .With(p => p.Gender, GenderOptions.Male)
                .With(p => p.Organizations, (List<string>?)null)
                .With(p => p.CurrentRoles, (List<string>?)null)
                .With(p => p.ConnectionChannels, (List<string>?)null)
                .With(p => p.UserDefinedTags, (List<string>?)null)
                .With(p => p.SystemStatusTags, new List<ContactsManger.Core.Domain.Entities.EEnums.EnSystemStatusTag> { requestedEnum })
                .With(p => p.SocialMediaAccounts, (List<SocialMediaAccountAddRequest>?)null)
                .Create();

            // Batched lookup: GetSystemStatusTagsByEnums replaces the old
            // per-enum GetSystemStatusTagByEnum call. No seeded row for the
            // requested enum -> empty result, tag gets skipped.
            _systemStatusTagRepositryContractMoq
                .Setup(repo => repo.GetSystemStatusTagsByEnums(It.Is<IEnumerable<ContactsManger.Core.Domain.Entities.EEnums.EnSystemStatusTag>>(ids => ids.Contains(requestedEnum))))
                .ReturnsAsync(new List<SystemStatusTag>());

            Person? captured = null;
            _personRepositryContractMoq
                .Setup(repo => repo.AddPerson(It.IsAny<Person>()))
                .Callback<Person>(p => captured = p)
                .ReturnsAsync((Person p) => p);

            // Act
            await _personAdderService.AddPerson(personAddRequest);

            // Assert
            captured.Should().NotBeNull();
            captured!.SystemStatusTags.Should().BeEmpty();
        }

        /// <summary>
        /// With the batched rewrite, multiple Organization names in one request
        /// must resolve via a SINGLE GetCirclesByNames call (not one call per
        /// name), and existing names must be reused rather than duplicated.
        /// </summary>
        [Fact]
        public async Task AddPerson_WithMixOfNewAndExistingOrganizations_BatchesIntoOneCall()
        {
            // Arrange
            var existingCircle = new Circle { CircleId = Guid.NewGuid(), Name = "Existing Org" };

            PersonAddRequest? personAddRequest = _fixture.Build<PersonAddRequest>()
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .With(p => p.Gender, GenderOptions.Male)
                .With(p => p.Organizations, new List<string> { "Existing Org", "New Org" })
                .With(p => p.CurrentRoles, (List<string>?)null)
                .With(p => p.ConnectionChannels, (List<string>?)null)
                .With(p => p.UserDefinedTags, (List<string>?)null)
                .With(p => p.SystemStatusTags, (List<ContactsManger.Core.Domain.Entities.EEnums.EnSystemStatusTag>?)null)
                .With(p => p.SocialMediaAccounts, (List<SocialMediaAccountAddRequest>?)null)
                .Create();

            _circleRepositryContractMoq
                .Setup(repo => repo.GetCirclesByNames(It.Is<IEnumerable<string>>(names =>
                    names.Contains("Existing Org") && names.Contains("New Org"))))
                .ReturnsAsync(new List<Circle> { existingCircle });

            Person? captured = null;
            _personRepositryContractMoq
                .Setup(repo => repo.AddPerson(It.IsAny<Person>()))
                .Callback<Person>(p => captured = p)
                .ReturnsAsync((Person p) => p);

            // Act
            await _personAdderService.AddPerson(personAddRequest);

            // Assert
            captured.Should().NotBeNull();
            captured!.Circles.Should().HaveCount(2);
            captured.Circles.Should().ContainSingle(c => c.CircleId == existingCircle.CircleId && c.Name == "Existing Org");
            captured.Circles.Should().ContainSingle(c => c.Name == "New Org" && c.CircleId != existingCircle.CircleId);

            // Exactly one batched call for the whole request, never per-name.
            _circleRepositryContractMoq.Verify(
                repo => repo.GetCirclesByNames(It.IsAny<IEnumerable<string>>()),
                Times.Once);
        }

        /// <summary>
        /// ApplicationUserId must come from ICurrentUserService.UserId (the
        /// authenticated user), never from client-submitted PersonAddRequest
        /// data -- PersonAddRequest has no ApplicationUserId field at all, by
        /// design, so a client can never claim ownership on someone else's
        /// behalf.
        /// </summary>
        [Fact]
        public async Task AddPerson_StampsApplicationUserIdFromCurrentUser()
        {
            // Arrange
            PersonAddRequest? personAddRequest = _fixture.Build<PersonAddRequest>()
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .With(p => p.Gender, GenderOptions.Male)
                .With(p => p.Organizations, (List<string>?)null)
                .With(p => p.CurrentRoles, (List<string>?)null)
                .With(p => p.ConnectionChannels, (List<string>?)null)
                .With(p => p.UserDefinedTags, (List<string>?)null)
                .With(p => p.SystemStatusTags, (List<ContactsManger.Core.Domain.Entities.EEnums.EnSystemStatusTag>?)null)
                .With(p => p.SocialMediaAccounts, (List<SocialMediaAccountAddRequest>?)null)
                .Create();

            Person? captured = null;
            _personRepositryContractMoq
                .Setup(repo => repo.AddPerson(It.IsAny<Person>()))
                .Callback<Person>(p => captured = p)
                .ReturnsAsync((Person p) => p);

            // Act
            await _personAdderService.AddPerson(personAddRequest);

            // Assert
            captured.Should().NotBeNull();
            captured!.ApplicationUserId.Should().Be(_testUserId);
        }

        /// <summary>
        /// If there is no authenticated user context (ICurrentUserService.UserId
        /// is null), AddPerson must refuse rather than silently persisting a
        /// Person with an empty/default ApplicationUserId.
        /// </summary>
        [Fact]
        public async Task AddPerson_NoAuthenticatedUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _currentUserServiceMoq.Setup(s => s.UserId).Returns((Guid?)null);

            PersonAddRequest? personAddRequest = _fixture.Build<PersonAddRequest>()
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .With(p => p.Gender, GenderOptions.Male)
                .With(p => p.Organizations, (List<string>?)null)
                .With(p => p.CurrentRoles, (List<string>?)null)
                .With(p => p.ConnectionChannels, (List<string>?)null)
                .With(p => p.UserDefinedTags, (List<string>?)null)
                .With(p => p.SystemStatusTags, (List<ContactsManger.Core.Domain.Entities.EEnums.EnSystemStatusTag>?)null)
                .With(p => p.SocialMediaAccounts, (List<SocialMediaAccountAddRequest>?)null)
                .Create();

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _personAdderService.AddPerson(personAddRequest));

            _personRepositryContractMoq.Verify(repo => repo.AddPerson(It.IsAny<Person>()), Times.Never);
        }

        #endregion

        #region PersonQuickAddRequest Tests

        [Fact]
        public async Task QuickAdd_NullRequest_ThrowsArgumentNullException()
        {
            // Arrange
            PersonQuickAddRequest? quickAddRequest = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _personQuickAdderService.QuickAddPerson(quickAddRequest));
        }

        [Fact]
        public async Task QuickAdd_MissingName_ThrowsArgumentException()
        {
            // Arrange
            PersonQuickAddRequest? quickAddRequest = _fixture.Build<PersonQuickAddRequest>()
                .With(p => p.Name, (string)null!)
                .With(p => p.email, "quick@example.com")
                .Create();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _personQuickAdderService.QuickAddPerson(quickAddRequest));
        }

        [Fact]
        public async Task QuickAdd_MissingEmail_ThrowsArgumentException()
        {
            // Arrange
            PersonQuickAddRequest? quickAddRequest = _fixture.Build<PersonQuickAddRequest>()
                .With(p => p.Name, "Quick Person")
                .With(p => p.email, (string)null!)
                .Create();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _personQuickAdderService.QuickAddPerson(quickAddRequest));
        }

        /// <summary>
        /// Quick Add intentionally does not require DateOfBirth, Gender, or
        /// CountryId -- those are Full Profile concerns. This confirms Quick Add
        /// succeeds without them, since PersonAddRequest would have rejected
        /// this same input.
        /// </summary>
        [Fact]
        public async Task QuickAdd_MinimalMandatoryFieldsOnly_Succeeds()
        {
            // Arrange
            PersonQuickAddRequest quickAddRequest = new PersonQuickAddRequest
            {
                Name = "Quick Person",
                email = "quick@example.com",
                Organizations = new List<string> { "Acme Inc" },
                CurrentRoles = new List<string> { "Engineer" },
                Origin = "Met at a meetup"
            };

            _circleRepositryContractMoq
                .Setup(repo => repo.GetCircleByName("Acme Inc"))
                .ReturnsAsync((Circle?)null);

            _personRepositryContractMoq
                .Setup(repo => repo.AddPerson(It.IsAny<Person>()))
                .ReturnsAsync((Person p) => p);

            // Act
            var result = await _personQuickAdderService.QuickAddPerson(quickAddRequest);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Quick Person");
            result.email.Should().Be("quick@example.com");
            result.PersonId.Should().NotBe(Guid.Empty);
            _circleRepositryContractMoq.Verify(repo => repo.GetCircleByName("Acme Inc"), Times.Once);
        }

        /// <summary>
        /// When an Organization name already matches an existing Circle, QuickAdd
        /// should reuse it instead of creating a duplicate.
        /// </summary>
        [Fact]
        public async Task QuickAdd_ExistingOrganization_ReusesExistingCircle()
        {
            // Arrange
            var existingCircle = new Circle { CircleId = Guid.NewGuid(), Name = "Acme Inc" };

            PersonQuickAddRequest quickAddRequest = new PersonQuickAddRequest
            {
                Name = "Quick Person",
                email = "quick2@example.com",
                Organizations = new List<string> { "Acme Inc" }
            };

            _circleRepositryContractMoq
                .Setup(repo => repo.GetCircleByName("Acme Inc"))
                .ReturnsAsync(existingCircle);

            Person? captured = null;
            _personRepositryContractMoq
                .Setup(repo => repo.AddPerson(It.IsAny<Person>()))
                .Callback<Person>(p => captured = p)
                .ReturnsAsync((Person p) => p);

            // Act
            await _personQuickAdderService.QuickAddPerson(quickAddRequest);

            // Assert
            captured.Should().NotBeNull();
            captured!.Circles.Should().ContainSingle(c => c.CircleId == existingCircle.CircleId);
        }

        /// <summary>
        /// Same contract as AddPerson: ApplicationUserId comes from the
        /// authenticated user via ICurrentUserService, never from client data.
        /// </summary>
        [Fact]
        public async Task QuickAdd_StampsApplicationUserIdFromCurrentUser()
        {
            // Arrange
            PersonQuickAddRequest quickAddRequest = new PersonQuickAddRequest
            {
                Name = "Quick Person",
                email = "quick3@example.com"
            };

            Person? captured = null;
            _personRepositryContractMoq
                .Setup(repo => repo.AddPerson(It.IsAny<Person>()))
                .Callback<Person>(p => captured = p)
                .ReturnsAsync((Person p) => p);

            // Act
            await _personQuickAdderService.QuickAddPerson(quickAddRequest);

            // Assert
            captured.Should().NotBeNull();
            captured!.ApplicationUserId.Should().Be(_testUserId);
        }

        [Fact]
        public async Task QuickAdd_NoAuthenticatedUser_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            _currentUserServiceMoq.Setup(s => s.UserId).Returns((Guid?)null);

            PersonQuickAddRequest quickAddRequest = new PersonQuickAddRequest
            {
                Name = "Quick Person",
                email = "quick4@example.com"
            };

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
                await _personQuickAdderService.QuickAddPerson(quickAddRequest));

            _personRepositryContractMoq.Verify(repo => repo.AddPerson(It.IsAny<Person>()), Times.Never);
        }

        #endregion

        #region GetPersonByPersonId Tests

        [Fact]
        public async Task GetPersonByPersonId_null()
        {
            // Arrange
            Guid? personId = null;

            // Act
            var k = await _personGetterService.GetPersonByPersonId(personId);

            // Assert
            Assert.Null(k);
        }

        [Fact]
        public async Task GetPersonByPersonID_WithPersonID_ToBeSuccessful()
        {
            // Arrange
            Person person = _fixture.Build<Person>()
                .With(temp => temp.email, "email@sample.com")
                .With(temp => temp.Country, null as Country)
                .Create();

            // Retained the original misspelled class and method names here:
            PersonRespones person_response_expected = person.ConvertToPersonRespons();

            _personRepositryContractMoq.Setup(temp => temp.GetPersonById(It.IsAny<Guid>()))
                .ReturnsAsync(person);

            // Act
            // Retained the misspelled return type here:
            PersonRespones? person_response_from_get = await _personGetterService.GetPersonByPersonId(person.PersonId);

            // Assert
            // PersonRespones.Equals only compares scalar fields (not the related
            // collections), which is what "Should().Be()" exercises here.
            person_response_from_get.Should().Be(person_response_expected);
        }

        /// <summary>
        /// Confirms related collections (Circles/Organizations, ContactItemRoles/
        /// CurrentRoles) are correctly mapped through GetPersonByPersonId -- this
        /// depends on PersonRepository having eagerly Include()'d these navigation
        /// properties.
        /// </summary>
        [Fact]
        public async Task GetPersonByPersonID_WithRelatedCollections_MapsCorrectly()
        {
            // Arrange
            Person person = _fixture.Build<Person>()
                .With(temp => temp.email, "email@sample.com")
                .With(temp => temp.Country, null as Country)
                .With(temp => temp.Circles, new List<Circle>
                {
                    new Circle { CircleId = Guid.NewGuid(), Name = "Acme Inc" }
                })
                .With(temp => temp.ContactItemRoles, new List<ContactItemRole>
                {
                    new ContactItemRole { ContactsRoleId = Guid.NewGuid(), Role = "Engineer" }
                })
                .Create();

            _personRepositryContractMoq.Setup(temp => temp.GetPersonById(It.IsAny<Guid>()))
                .ReturnsAsync(person);

            // Act
            PersonRespones? result = await _personGetterService.GetPersonByPersonId(person.PersonId);

            // Assert
            result.Should().NotBeNull();
            result!.Organizations.Should().ContainSingle(o => o.Name == "Acme Inc");
            result.CurrentRoles.Should().ContainSingle(r => r.Role == "Engineer");
        }

        #endregion

        #region GetAllPersons Tests

        [Fact]
        public async Task GetAllPersons_Test()
        {
            // Arrange
            List<Person> persons = new List<Person>()
            {
                _fixture.Build<Person>()
                    .With(p => p.email, "p1@example.com")
                    .With(p => p.phone, "123456789")
                    .With(p => p.Country, null as Country)
                    .Create(),

                _fixture.Build<Person>()
                    .With(p => p.email, "p2@example.com")
                    .With(p => p.phone, "123456789")
                    .With(p => p.Country, null as Country)
                    .Create(),

                _fixture.Build<Person>()
                    .With(p => p.email, "p3@example.com")
                    .With(p => p.phone, "123456789")
                    .With(p => p.Country, null as Country)
                    .Create()
            };

            _personRepositryContractMoq.Setup(repo => repo.GetAllPersons()).ReturnsAsync(persons);

            List<PersonRespones> expected_persons = persons.Select(p => p.ConvertToPersonRespons()).ToList();

            // Act
            List<PersonRespones> actual_persons = await _personGetterService.GetAllPersons();

            // Assert
            actual_persons.Should().BeEquivalentTo(expected_persons);
        }

        /// <summary>
        /// PersonGetterService does no filtering of its own for IsDeleted -- that
        /// exclusion happens via the EF Core global query filter
        /// (HasQueryFilter(p => !p.IsDeleted)) inside PersonRepository. This test
        /// documents that contract: whatever the repository returns is trusted
        /// as-is, so a soft-deleted Person the repository fails to filter out
        /// would leak straight through to the response -- this is a Repository
        /// concern, not a Service concern, and this test exists so that
        /// assumption stays visible.
        /// </summary>
        [Fact]
        public async Task GetAllPersons_TrustsRepositoryToExcludeSoftDeletedRows()
        {
            // Arrange
            Person activePerson = _fixture.Build<Person>()
                .With(p => p.email, "active@example.com")
                .With(p => p.phone, "123456789")
                .With(p => p.Country, null as Country)
                .With(p => p.IsDeleted, false)
                .Create();

            // Repository is mocked to already reflect the query filter's effect --
            // i.e. it never returns a soft-deleted person in the first place.
            _personRepositryContractMoq
                .Setup(repo => repo.GetAllPersons())
                .ReturnsAsync(new List<Person> { activePerson });

            // Act
            List<PersonRespones> result = await _personGetterService.GetAllPersons();

            // Assert
            result.Should().ContainSingle(p => p.PersonId == activePerson.PersonId);
        }

        #endregion

        #region SearchBy Tests

        [Fact]
        public async Task GetPersonsByName_EmptySearchText_ToBeSuccessful()
        {
            // Arrange
            List<Person> persons = new List<Person>()
            {
                _fixture.Build<Person>()
                    .With(p => p.email, "p1@example.com")
                    .With(p => p.phone, "123456789")
                    .With(p => p.Country, null as Country)
                    .Create(),

                _fixture.Build<Person>()
                    .With(p => p.email, "p2@example.com")
                    .With(p => p.phone, "123456789")
                    .With(p => p.Country, null as Country)
                    .Create(),

                _fixture.Build<Person>()
                    .With(p => p.email, "p3@example.com")
                    .With(p => p.phone, "123456789")
                    .With(p => p.Country, null as Country)
                    .Create()
            };

            // SearchPersonsBy delegates to GetFilteredPersons(predicate), not
            // GetAllPersons -- must mock the method actually called.
            //
            // NOTE: the real signature is SearchPersonsBy(string? PersonParamter,
            // string SearchBy) -- the search TEXT comes first, the field-name
            // switch key comes second. Passing them in the wrong order (as the
            // original test did) makes SearchBy land on an unrecognized value,
            // which falls through to the default branch (GetAllPersons) instead
            // of the Name case (GetFilteredPersons) -- causing this test to
            // silently exercise the wrong code path.
            _personRepositryContractMoq
                .Setup(repo => repo.GetFilteredPersons(It.IsAny<Expression<Func<Person, bool>>>()))
                .ReturnsAsync(persons);

            // Act
            List<PersonRespones> actualList = await _personSearcherService.SearchPersonsBy(string.Empty, nameof(Person.Name));

            // Assert
            actualList.Should().BeEquivalentTo(persons.Select(p => p.ConvertToPersonRespons()).ToList());
        }

        [Fact]
        public async Task GetFilteredPersons_EmptySearchText_ToBeSuccess()
        {
            // Arrange
            List<Person> persons = new List<Person>()
            {
                _fixture.Build<Person>()
                    .With(p => p.email, "p1@example.com")
                    .With(p => p.phone, "123456789")
                    .With(p => p.Country, null as Country)
                    .Create(),

                _fixture.Build<Person>()
                    .With(p => p.email, "p2@example.com")
                    .With(p => p.phone, "123456789")
                    .With(p => p.Country, null as Country)
                    .Create(),

                _fixture.Build<Person>()
                    .With(p => p.email, "p3@example.com")
                    .With(p => p.phone, "123456789")
                    .With(p => p.Country, null as Country)
                    .Create()
            };

            _personRepositryContractMoq
                .Setup(repo => repo.GetFilteredPersons(It.IsAny<Expression<Func<Person, bool>>>()))
                .ReturnsAsync(persons);

            // Act
            // Search text first, field-name key second -- see note above.
            List<PersonRespones> actualList = await _personSearcherService.SearchPersonsBy("sa", nameof(Person.Name));

            // Assert
            actualList.Should().BeEquivalentTo(persons.Select(p => p.ConvertToPersonRespons()).ToList());
        }

        #endregion

        #region GetPersonSorted Tests

        [Fact]
        public async Task GetPersonsSorted_DESC_Test()
        {
            // Arrange
            List<Person> persons = new List<Person>()
            {
                _fixture.Build<Person>()
                    .With(p => p.email, "p1@example.com")
                    .With(p => p.phone, "123456789")
                    .With(p => p.Country, null as Country)
                    .Create(),

                _fixture.Build<Person>()
                    .With(p => p.email, "p2@example.com")
                    .With(p => p.phone, "123456789")
                    .With(p => p.Country, null as Country)
                    .Create(),

                _fixture.Build<Person>()
                    .With(p => p.email, "p3@example.com")
                    .With(p => p.phone, "123456789")
                    .With(p => p.Country, null as Country)
                    .Create()
            };

            var personToSort = persons.Select(p => p.ConvertToPersonViewDTO()).ToList();

            // Act
            List<PersonViewDTO> actualList = await _personSorterService.getPersonsSorted(personToSort, nameof(Person.Name), sortedListOp.Descending);

            // Assert
            actualList.Should().BeInDescendingOrder(p => p.Name);
        }

        #endregion

        #region UpdatePerson Tests

        [Fact]
        public async Task UpdatePerson_Null_Test()
        {
            // Arrange
            PersonUpdateRequest? personUpdateRequest = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _personUpdaterService.UpdatePerson(personUpdateRequest));
        }

        [Fact]
        public async Task UpdatePerson_ProperDetails_IdIsNull_Test()
        {
            // Arrange
            PersonUpdateRequest personUpdateRequest = _fixture.Build<PersonUpdateRequest>()
                .With(p => p.PersonId, (Guid?)null)
                .With(p => p.Name, (string?)null)
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .Create();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await _personUpdaterService.UpdatePerson(personUpdateRequest));
        }

        [Fact]
        public async Task UpdatePerson_NonExistentPersonId_ThrowsArgumentException()
        {
            // Arrange
            PersonUpdateRequest personUpdateRequest = _fixture.Build<PersonUpdateRequest>()
                .With(p => p.PersonId, Guid.NewGuid())
                .With(p => p.Name, "Some Name")
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .With(p => p.Gender, GenderOptions.Male)
                .With(p => p.Organizations, (List<string>?)null)
                .With(p => p.CurrentRoles, (List<string>?)null)
                .With(p => p.ConnectionChannels, (List<string>?)null)
                .With(p => p.UserDefinedTags, (List<string>?)null)
                .With(p => p.SystemStatusTags, (List<ContactsManger.Core.Domain.Entities.EEnums.EnSystemStatusTag>?)null)
                .With(p => p.SocialMediaAccounts, (List<SocialMediaAccountAddRequest>?)null)
                .Create();

            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonById(It.IsAny<Guid>()))
                .ReturnsAsync((Person?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () => await _personUpdaterService.UpdatePerson(personUpdateRequest));
        }

        [Fact]
        public async Task UpdatePerson_ProperDetails_Test()
        {
            // Arrange
            Person person = _fixture.Build<Person>()
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .Without(p => p.Country)
                .With(p => p.Gender, "Male")
                .Create();

            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonById(It.IsAny<Guid>()))
                .ReturnsAsync(person);

            _personRepositryContractMoq
                .Setup(repo => repo.UpdatePerson(It.IsAny<Person>()))
                .ReturnsAsync((Person p) => p);

            PersonUpdateRequest toUpdate = person.ConvertToPersonRespons().ToPersonUpdateRequest();
            toUpdate.Name = "karim";
            toUpdate.DateOfBirth = new DateTime(1990, 1, 1);
            toUpdate.Organizations = null;
            toUpdate.CurrentRoles = null;
            toUpdate.ConnectionChannels = null;
            toUpdate.UserDefinedTags = null;
            toUpdate.SystemStatusTags = null;
            toUpdate.SocialMediaAccounts = null;

            // Act
            PersonRespones? result = await _personUpdaterService.UpdatePerson(toUpdate);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("karim");
            result.DateOfBirth.Should().Be(new DateTime(1990, 1, 1));
        }

        /// <summary>
        /// Covers the full-profile scalar fields flowing through PersonUpdateRequest.
        /// </summary>
        [Fact]
        public async Task UpdatePerson_WithProfileFields_UpdatesScalarFieldsCorrectly()
        {
            // Arrange
            Person person = _fixture.Build<Person>()
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .Without(p => p.Country)
                .With(p => p.Gender, "Male")
                .Create();

            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonById(It.IsAny<Guid>()))
                .ReturnsAsync(person);

            _personRepositryContractMoq
                .Setup(repo => repo.UpdatePerson(It.IsAny<Person>()))
                .ReturnsAsync((Person p) => p);

            PersonUpdateRequest toUpdate = person.ConvertToPersonRespons().ToPersonUpdateRequest();
            toUpdate.ContextMemory = "Updated context memory";
            toUpdate.ProfileImagePath = "/images/updated.png";
            toUpdate.Origin = "Updated origin";
            toUpdate.LinkedInProfile = "https://linkedin.com/in/updated";
            toUpdate.OtherInformation = "Updated other info";
            toUpdate.Organizations = null;
            toUpdate.CurrentRoles = null;
            toUpdate.ConnectionChannels = null;
            toUpdate.UserDefinedTags = null;
            toUpdate.SystemStatusTags = null;
            toUpdate.SocialMediaAccounts = null;

            // Act
            PersonRespones? result = await _personUpdaterService.UpdatePerson(toUpdate);

            // Assert
            result.Should().NotBeNull();
            result!.ContextMemory.Should().Be(toUpdate.ContextMemory);
            result.ProfileImagePath.Should().Be(toUpdate.ProfileImagePath);
            result.Origin.Should().Be(toUpdate.Origin);
            result.LinkedInProfile.Should().Be(toUpdate.LinkedInProfile);
            result.OtherInformation.Should().Be(toUpdate.OtherInformation);
        }

        /// <summary>
        /// UpdatePerson clears and re-resolves Organizations via get-or-create
        /// against CircleRepositryContract, mirroring AddPerson's behavior.
        /// </summary>
        [Fact]
        public async Task UpdatePerson_ReplacingOrganizations_ResolvesNewCircle()
        {
            // Arrange
            Person person = _fixture.Build<Person>()
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .Without(p => p.Country)
                .With(p => p.Gender, "Male")
                .With(p => p.Circles, new List<Circle> { new Circle { CircleId = Guid.NewGuid(), Name = "Old Org" } })
                .Create();

            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonById(It.IsAny<Guid>()))
                .ReturnsAsync(person);

            // Batched lookup: PersonUpdaterService was rewritten to use
            // GetCirclesByNames the same way PersonAdderService was.
            _circleRepositryContractMoq
                .Setup(repo => repo.GetCirclesByNames(It.Is<IEnumerable<string>>(names => names.Contains("New Org"))))
                .ReturnsAsync(new List<Circle>());

            Person? captured = null;
            _unitOfWorkMoq
                .Setup(u => u.SaveChangesAsync())
                .Callback(() => captured = person)
                .ReturnsAsync(1);

            PersonUpdateRequest toUpdate = person.ConvertToPersonRespons().ToPersonUpdateRequest();
            toUpdate.Organizations = new List<string> { "New Org" };
            toUpdate.CurrentRoles = null;
            toUpdate.ConnectionChannels = null;
            toUpdate.UserDefinedTags = null;
            toUpdate.SystemStatusTags = null;
            toUpdate.SocialMediaAccounts = null;

            // Act
            await _personUpdaterService.UpdatePerson(toUpdate);

            // Assert
            captured.Should().NotBeNull();
            captured!.Circles.Should().ContainSingle(c => c.Name == "New Org");
            captured.Circles.Should().NotContain(c => c.Name == "Old Org");
        }

        #endregion

        #region DeletePersonByPersonId Tests

        [Fact]
        public async Task DeletePersonByPersonId_Null_Test()
        {
            // Arrange
            Guid? personId = null;

            // Act & Assert
            Assert.False(await _personDeleterService.DeletePersonByPersonId(personId));
        }

        /// <summary>
        /// PersonRepository.DeletePerson now performs a SOFT delete (flips
        /// IsDeleted = true) rather than removing the row. At the Service layer
        /// this test only cares about the true/false contract at the repository
        /// boundary -- PersonDeleterService itself is unchanged by the switch to
        /// soft delete, since that logic lives entirely in the Repository.
        /// </summary>
        [Fact]
        public async Task DeletePersonByPersonId_ValidTest_Test()
        {
            // Arrange
            Person person = _fixture.Build<Person>()
                 .With(p => p.PersonId, Guid.NewGuid())
                 .With(p => p.email, "test@example.com")
                 .With(p => p.phone, "123456789")
                 .Without(p => p.Country)
                 .Create();

            _personRepositryContractMoq
                .Setup(repo => repo.DeletePerson(person.PersonId))
                .ReturnsAsync(true);

            // Act
            bool isTrue = await _personDeleterService.DeletePersonByPersonId(person.PersonId);

            // Assert
            Assert.True(isTrue);
        }

        [Fact]
        public async Task DeletePersonByPersonId_InValidTest_Test()
        {
            // Arrange
            Guid nonExistentId = Guid.NewGuid();

            _personRepositryContractMoq
                .Setup(repo => repo.DeletePerson(nonExistentId))
                .ReturnsAsync(false);

            // Act
            bool isfalse = await _personDeleterService.DeletePersonByPersonId(nonExistentId);

            // Assert
            Assert.False(isfalse);
        }

        /// <summary>
        /// With soft delete, calling DeletePerson twice on the same PersonId must
        /// return false the second time (already deleted), same contract as
        /// "not found" -- the Repository's IsDeleted guard covers this, and the
        /// Service must pass that false straight through without treating it as
        /// an error.
        /// </summary>
        [Fact]
        public async Task DeletePersonByPersonId_AlreadyDeleted_ReturnsFalse()
        {
            // Arrange
            Guid personId = Guid.NewGuid();

            _personRepositryContractMoq
                .SetupSequence(repo => repo.DeletePerson(personId))
                .ReturnsAsync(true)   // first call: soft-deletes successfully
                .ReturnsAsync(false); // second call: already IsDeleted -> false

            // Act
            bool firstResult = await _personDeleterService.DeletePersonByPersonId(personId);
            bool secondResult = await _personDeleterService.DeletePersonByPersonId(personId);

            // Assert
            Assert.True(firstResult);
            Assert.False(secondResult);
        }

        #endregion
    }
}