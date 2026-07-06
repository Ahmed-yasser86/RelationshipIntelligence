using AutoFixture;
using AutoFixture.Kernel;
using ContactsManger.Core.DTOs.PersonDTOs;
using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts.DTOs;
using Servicess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    /// <summary>
    /// Isolated test suite for the paginated "load N contacts, then load the
    /// next N" feature (PersonGetterService.GetPersonsViewBatched +
    /// PersonRepositryContract.GetPersonsPaged). Kept separate from
    /// PersonServicesTest since this exercises a distinct feature surface
    /// (PagedResult&lt;PersonViewDTO&gt;) rather than the PersonRespones-based
    /// CRUD flows covered there.
    ///
    /// PagedResult&lt;T&gt;.HasMore is a computed property
    /// ((long)PageNumber * PageSize &lt; TotalCount), not a settable field --
    /// every assertion below only ever reads it, matching the real type.
    /// </summary>
    public class PersonPaginationTest
    {
        private readonly IFixture _fixture;
        private readonly Mock<PersonRepositryContract> _personRepositryContractMoq;
        private readonly PersonGetterService _personGetterService;

        public PersonPaginationTest()
        {
            _fixture = new Fixture();

            // Same recursion guard as PersonServicesTest -- Person's navigation
            // collections (Circles, ContactItemRoles, etc.) create a circular
            // graph that AutoFixture's default behavior throws on.
            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
                .ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            _personRepositryContractMoq = new Mock<PersonRepositryContract>();

            var loggerMock = new Mock<ILogger<PersonGetterService>>();
            _personGetterService = new PersonGetterService(_personRepositryContractMoq.Object, loggerMock.Object);
        }

        /// <summary>
        /// Builds plain Persons with every navigation collection explicitly
        /// emptied. Without this, AutoFixture (even under OmitOnRecursionBehavior)
        /// will still populate Circles/ContactItemRoles/ConnectionChannels/
        /// SystemStatusTags/UserDefinedTags/Interactions with fixture-generated
        /// entities, which is harmless for tests that don't assert on them but
        /// adds noise and slows fixture generation across dozens of Persons.
        /// </summary>
        private List<Person> BuildPersons(int count)
        {
            var persons = new List<Person>();
            for (int i = 0; i < count; i++)
            {
                persons.Add(_fixture.Build<Person>()
                    .With(p => p.email, $"person{i}@example.com")
                    .With(p => p.phone, "123456789")
                    .With(p => p.Country, null as Country)
                    .With(p => p.Circles, new List<ContactsManger.Core.Domain.Entities.Circle>())
                    .With(p => p.ContactItemRoles, new List<ContactsManger.Core.Domain.Entities.ContactItemRole>())
                    .With(p => p.ConnectionChannels, new List<ContactsManger.Core.Domain.Entities.ConnectionChannel>())
                    .With(p => p.SystemStatusTags, new List<ContactsManger.Core.Domain.Entities.SystemStatusTag>())
                    .With(p => p.UserDefinedTags, new List<ContactsManger.Core.Domain.Entities.UserDefinedTags>())
                    .With(p => p.Interactions, new List<ContactsManger.Core.Domain.Entities.Interaction>())
                    .With(p => p.Notes, new List<ContactsManger.Core.Domain.Entities.Note>())
                    .With(p => p.OtherSocialMediaAccounts, new List<ContactsManger.Core.Domain.Entities.SocialMediaAccount>())
                    .Create());
            }
            return persons;
        }

        #region Page number / page size normalization

        /// <summary>
        /// A PageNumber of 0 or negative is not a valid 1-based page. The
        /// service must clamp it to 1 rather than passing a nonsensical value
        /// down to the repository (which would translate into an invalid or
        /// negative SQL OFFSET).
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-100)]
        public async Task GetPersonsViewBatched_InvalidPageNumber_ClampsToOne(int invalidPageNumber)
        {
            // Arrange
            var persons = BuildPersons(3);

            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonsPaged(1, It.IsAny<int>()))
                .ReturnsAsync((persons, persons.Count));

            // Act
            PagedResult<PersonViewDTO> result = await _personGetterService.GetPersonsViewBatched(invalidPageNumber, 10);

            // Assert
            result.PageNumber.Should().Be(1);
            _personRepositryContractMoq.Verify(repo => repo.GetPersonsPaged(1, It.IsAny<int>()), Times.Once);
        }

        /// <summary>
        /// A PageSize of 0 or negative must clamp to 1 rather than being
        /// forwarded as-is -- a zero or negative Take() is either a no-op or
        /// throws depending on the LINQ provider, neither of which is a sane
        /// response to a malformed frontend request.
        /// </summary>
        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task GetPersonsViewBatched_InvalidPageSize_ClampsToOne(int invalidPageSize)
        {
            // Arrange
            var persons = BuildPersons(1);

            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonsPaged(It.IsAny<int>(), 1))
                .ReturnsAsync((persons, persons.Count));

            // Act
            PagedResult<PersonViewDTO> result = await _personGetterService.GetPersonsViewBatched(1, invalidPageSize);

            // Assert
            result.PageSize.Should().Be(1);
            _personRepositryContractMoq.Verify(repo => repo.GetPersonsPaged(It.IsAny<int>(), 1), Times.Once);
        }

        #endregion

        #region Correct pagination behavior

        [Fact]
        public async Task GetPersonsViewBatched_FirstPage_ReturnsRequestedPageSize()
        {
            // Arrange
            const int pageSize = 5;
            const int totalCount = 23;
            var pageOneItems = BuildPersons(pageSize);

            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonsPaged(1, pageSize))
                .ReturnsAsync((pageOneItems, totalCount));

            // Act
            PagedResult<PersonViewDTO> result = await _personGetterService.GetPersonsViewBatched(1, pageSize);

            // Assert
            result.Items.Should().HaveCount(pageSize);
            result.TotalCount.Should().Be(totalCount);
            result.PageNumber.Should().Be(1);
            result.PageSize.Should().Be(pageSize);
        }

        /// <summary>
        /// Confirms the service passes pageNumber/pageSize straight through to
        /// the repository unchanged (when valid) -- pagination math (the actual
        /// Skip/Take offset calculation) belongs entirely to the Repository,
        /// not duplicated here in the Service.
        /// </summary>
        [Fact]
        public async Task GetPersonsViewBatched_SecondPage_RequestsCorrectPageFromRepository()
        {
            // Arrange
            const int pageNumber = 2;
            const int pageSize = 10;
            var pageTwoItems = BuildPersons(10);

            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonsPaged(pageNumber, pageSize))
                .ReturnsAsync((pageTwoItems, 35));

            // Act
            PagedResult<PersonViewDTO> result = await _personGetterService.GetPersonsViewBatched(pageNumber, pageSize);

            // Assert
            result.Items.Should().HaveCount(10);
            _personRepositryContractMoq.Verify(repo => repo.GetPersonsPaged(pageNumber, pageSize), Times.Once);
        }

        /// <summary>
        /// HasMore is computed as (long)PageNumber * PageSize &lt; TotalCount.
        /// Must be true when there are more contacts beyond the current page --
        /// this is what drives the frontend's "load more" button visibility.
        /// </summary>
        [Fact]
        public async Task GetPersonsViewBatched_WhenMoreRecordsRemain_HasMoreIsTrue()
        {
            // Arrange
            var items = BuildPersons(10);

            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonsPaged(1, 10))
                .ReturnsAsync((items, 25)); // 10 shown, 15 remaining -> 1*10 < 25

            // Act
            PagedResult<PersonViewDTO> result = await _personGetterService.GetPersonsViewBatched(1, 10);

            // Assert
            result.HasMore.Should().BeTrue();
        }

        /// <summary>
        /// HasMore must be false on the last page, even when that page is
        /// exactly full (e.g. 20 total, page size 10, page 2 -> 2*10 == 20,
        /// not less-than, so HasMore is false). This is the boundary case
        /// most off-by-one bugs hide in -- confirmed here against the real
        /// strict-inequality formula in PagedResult&lt;T&gt;.
        /// </summary>
        [Fact]
        public async Task GetPersonsViewBatched_OnExactLastPage_HasMoreIsFalse()
        {
            // Arrange
            var items = BuildPersons(10);

            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonsPaged(2, 10))
                .ReturnsAsync((items, 20)); // page 2 of exactly 20 total -> 2*10 == 20

            // Act
            PagedResult<PersonViewDTO> result = await _personGetterService.GetPersonsViewBatched(2, 10);

            // Assert
            result.HasMore.Should().BeFalse();
        }

        /// <summary>
        /// HasMore must be false when the final page is a PARTIAL page
        /// (e.g. 23 total, page size 10 -> page 3 has only 3 items,
        /// 3*10 == 30 which is already &gt;= 23).
        /// </summary>
        [Fact]
        public async Task GetPersonsViewBatched_OnPartialLastPage_HasMoreIsFalse()
        {
            // Arrange
            var items = BuildPersons(3);

            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonsPaged(3, 10))
                .ReturnsAsync((items, 23)); // page 3 of 23 total, only 3 items on this page

            // Act
            PagedResult<PersonViewDTO> result = await _personGetterService.GetPersonsViewBatched(3, 10);

            // Assert
            result.Items.Should().HaveCount(3);
            result.HasMore.Should().BeFalse();
        }

        [Fact]
        public async Task GetPersonsViewBatched_NoPersonsExist_ReturnsEmptyPageWithZeroTotal()
        {
            // Arrange
            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonsPaged(1, 10))
                .ReturnsAsync((new List<Person>(), 0));

            // Act
            PagedResult<PersonViewDTO> result = await _personGetterService.GetPersonsViewBatched(1, 10);

            // Assert
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.HasMore.Should().BeFalse(); // 1*10 < 0 is false
        }

        #endregion

        #region Mapping correctness (Person -> PersonViewDTO)

        /// <summary>
        /// GetPersonsViewBatched must map through PersonViewDTO.ConvertToPersonViewDTO()
        /// (the lightweight projection extension), NOT PersonRespones -- confirms
        /// the correct converter is wired up and related collections carry through
        /// correctly. Matches the real PersonViewDTO shape: Organizations
        /// (List&lt;CircleResponse&gt;), CurrentRoles (List&lt;ContactItemRoleResponse&gt;),
        /// ConnectionChannels, SystemStatusTags, UserDefinedTags, Interactions.
        /// </summary>
        [Fact]
        public async Task GetPersonsViewBatched_MapsRelatedCollectionsIntoPersonViewDTO()
        {
            // Arrange
            var country = new Country { CountryId = Guid.NewGuid(), CountryName = "Egypt" };

            var person = _fixture.Build<Person>()
                .With(p => p.email, "test@example.com")
                .With(p => p.phone, "123456789")
                .With(p => p.Country, country)
                .With(p => p.Circles, new List<ContactsManger.Core.Domain.Entities.Circle>
                {
                    new ContactsManger.Core.Domain.Entities.Circle { CircleId = Guid.NewGuid(), Name = "Acme Inc" }
                })
                .With(p => p.ContactItemRoles, new List<ContactsManger.Core.Domain.Entities.ContactItemRole>
                {
                    new ContactsManger.Core.Domain.Entities.ContactItemRole { ContactsRoleId = Guid.NewGuid(), Role = "Engineer" }
                })
                .With(p => p.ConnectionChannels, new List<ContactsManger.Core.Domain.Entities.ConnectionChannel>())
                .With(p => p.SystemStatusTags, new List<ContactsManger.Core.Domain.Entities.SystemStatusTag>())
                .With(p => p.UserDefinedTags, new List<ContactsManger.Core.Domain.Entities.UserDefinedTags>())
                .With(p => p.Interactions, new List<ContactsManger.Core.Domain.Entities.Interaction>())
                .With(p => p.Notes, new List<ContactsManger.Core.Domain.Entities.Note>())
                .With(p => p.OtherSocialMediaAccounts, new List<ContactsManger.Core.Domain.Entities.SocialMediaAccount>())
                .Create();

            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonsPaged(1, 10))
                .ReturnsAsync((new List<Person> { person }, 1));

            // Act
            PagedResult<PersonViewDTO> result = await _personGetterService.GetPersonsViewBatched(1, 10);

            // Assert
            result.Items.Should().ContainSingle();
            PersonViewDTO dto = result.Items.Single();
            dto.PersonId.Should().Be(person.PersonId);
            dto.CountryName.Should().Be("Egypt");
            dto.Organizations.Should().ContainSingle(o => o.Name == "Acme Inc");
            dto.CurrentRoles.Should().ContainSingle(r => r.Role == "Engineer");
        }

        #endregion

        #region Ordering / determinism

        /// <summary>
        /// Skip/Take is only deterministic across pages if the underlying query
        /// has a stable ORDER BY. This test doesn't (and can't, at the mocked
        /// Repository boundary) verify the SQL ordering itself, but it documents
        /// the contract: the Service trusts whatever order the Repository
        /// returns items in and does not re-sort or shuffle them.
        /// </summary>
        [Fact]
        public async Task GetPersonsViewBatched_PreservesRepositoryReturnOrder()
        {
            // Arrange
            var p1 = _fixture.Build<Person>()
                .With(p => p.Name, "Alice")
                .With(p => p.Country, null as Country)
                .With(p => p.email, "a@example.com")
                .With(p => p.phone, "1")
                .With(p => p.Circles, new List<ContactsManger.Core.Domain.Entities.Circle>())
                .With(p => p.ContactItemRoles, new List<ContactsManger.Core.Domain.Entities.ContactItemRole>())
                .With(p => p.ConnectionChannels, new List<ContactsManger.Core.Domain.Entities.ConnectionChannel>())
                .With(p => p.SystemStatusTags, new List<ContactsManger.Core.Domain.Entities.SystemStatusTag>())
                .With(p => p.UserDefinedTags, new List<ContactsManger.Core.Domain.Entities.UserDefinedTags>())
                .With(p => p.Interactions, new List<ContactsManger.Core.Domain.Entities.Interaction>())
                .With(p => p.Notes, new List<ContactsManger.Core.Domain.Entities.Note>())
                .With(p => p.OtherSocialMediaAccounts, new List<ContactsManger.Core.Domain.Entities.SocialMediaAccount>())
                .Create();

            var p2 = _fixture.Build<Person>()
                .With(p => p.Name, "Bob")
                .With(p => p.Country, null as Country)
                .With(p => p.email, "b@example.com")
                .With(p => p.phone, "2")
                .With(p => p.Circles, new List<ContactsManger.Core.Domain.Entities.Circle>())
                .With(p => p.ContactItemRoles, new List<ContactsManger.Core.Domain.Entities.ContactItemRole>())
                .With(p => p.ConnectionChannels, new List<ContactsManger.Core.Domain.Entities.ConnectionChannel>())
                .With(p => p.SystemStatusTags, new List<ContactsManger.Core.Domain.Entities.SystemStatusTag>())
                .With(p => p.UserDefinedTags, new List<ContactsManger.Core.Domain.Entities.UserDefinedTags>())
                .With(p => p.Interactions, new List<ContactsManger.Core.Domain.Entities.Interaction>())
                .With(p => p.Notes, new List<ContactsManger.Core.Domain.Entities.Note>())
                .With(p => p.OtherSocialMediaAccounts, new List<ContactsManger.Core.Domain.Entities.SocialMediaAccount>())
                .Create();

            var p3 = _fixture.Build<Person>()
                .With(p => p.Name, "Charlie")
                .With(p => p.Country, null as Country)
                .With(p => p.email, "c@example.com")
                .With(p => p.phone, "3")
                .With(p => p.Circles, new List<ContactsManger.Core.Domain.Entities.Circle>())
                .With(p => p.ContactItemRoles, new List<ContactsManger.Core.Domain.Entities.ContactItemRole>())
                .With(p => p.ConnectionChannels, new List<ContactsManger.Core.Domain.Entities.ConnectionChannel>())
                .With(p => p.SystemStatusTags, new List<ContactsManger.Core.Domain.Entities.SystemStatusTag>())
                .With(p => p.UserDefinedTags, new List<ContactsManger.Core.Domain.Entities.UserDefinedTags>())
                .With(p => p.Interactions, new List<ContactsManger.Core.Domain.Entities.Interaction>())
                .With(p => p.Notes, new List<ContactsManger.Core.Domain.Entities.Note>())
                .With(p => p.OtherSocialMediaAccounts, new List<ContactsManger.Core.Domain.Entities.SocialMediaAccount>())
                .Create();

            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonsPaged(1, 10))
                .ReturnsAsync((new List<Person> { p1, p2, p3 }, 3));

            // Act
            PagedResult<PersonViewDTO> result = await _personGetterService.GetPersonsViewBatched(1, 10);

            // Assert
            result.Items.Select(p => p.Name).Should().ContainInOrder("Alice", "Bob", "Charlie");
        }

        #endregion

        #region Exception handling

        [Fact]
        public async Task GetPersonsViewBatched_RepositoryThrows_ExceptionPropagates()
        {
            // Arrange
            _personRepositryContractMoq
                .Setup(repo => repo.GetPersonsPaged(It.IsAny<int>(), It.IsAny<int>()))
                .ThrowsAsync(new InvalidOperationException("Simulated database failure"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _personGetterService.GetPersonsViewBatched(1, 10));
        }

        #endregion
    }
}