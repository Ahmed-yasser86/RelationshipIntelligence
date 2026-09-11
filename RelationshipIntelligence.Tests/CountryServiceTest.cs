using AutoFixture;
using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using ServiceContracts.DTOs;
using Servicess;

namespace CRUDTests
{

    public class CountryServiceTest
    {
        private readonly Fixture _fixture;

        // Repositories
        private readonly Mock<CountryRepositryContract> _countryRepositryContractMoq;
        private readonly CountryRepositryContract _countryRepositryContract;

        // Unit of Work
        private readonly Mock<IUnitOfWork> _unitOfWorkMoq;
        private readonly IUnitOfWork _unitOfWork;

        // Services
        private readonly CountryGetterService _countryGetterService;
        private readonly CountryAdderService _countryAdderService;

        public CountryServiceTest()
        {
            _fixture = new Fixture();

            _fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
                .ToList()
                .ForEach(b => _fixture.Behaviors.Remove(b));
            _fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            _countryRepositryContractMoq = new Mock<CountryRepositryContract>();
            _countryRepositryContract = _countryRepositryContractMoq.Object;


            _unitOfWorkMoq = new Mock<IUnitOfWork>();
            _unitOfWork = _unitOfWorkMoq.Object;

            ILogger<CountryGetterService> loggerGetter = new Mock<ILogger<CountryGetterService>>().Object;
            ILogger<CountryAdderService> loggerAdder = new Mock<ILogger<CountryAdderService>>().Object;


            _countryGetterService = new CountryGetterService(_countryRepositryContract, loggerGetter);
            _countryAdderService = new CountryAdderService(_countryRepositryContract, loggerAdder, _unitOfWork);
        }


        #region AddCountryRequest Tests

        [Fact]
        public async Task AddCountryRequest_CountryNameNullValue_ThrowsArgumentException()
        {
            // Arrange
            var countryAddRequest = _fixture.Build<CountryAddRequest>()
                .With(c => c.CountryName, (string?)null)
                .Create();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _countryAdderService.AddCountryRequest(countryAddRequest));
        }

        [Fact]
        public async Task AddCountryRequest_NullValue_ThrowsArgumentNullException()
        {
            // Arrange
            CountryAddRequest? countryAddRequest = null;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await _countryAdderService.AddCountryRequest(countryAddRequest));
        }

        [Fact]
        public async Task AddCountryRequest_ProperCountryDetail_Success()
        {
            // Debug: Print to verify mock is being used
            Console.WriteLine("Test starting...");

            // Arrange
            string uniqueCountryName = $"Japan_{Guid.NewGuid()}";

            var countryAddRequest = new CountryAddRequest
            {
                CountryName = uniqueCountryName
            };

            Country expectedCountry = new Country
            {
                CountryId = Guid.NewGuid(),
                CountryName = uniqueCountryName
            };

            // Setup mock to return null
            _countryRepositryContractMoq
                .Setup(repo => repo.GetCountryByName(It.IsAny<string>()))
                .ReturnsAsync((Country?)null)
                .Callback(() => Console.WriteLine("GetCountryByName was called!"));

            _countryRepositryContractMoq
                .Setup(repo => repo.AddCountry(It.IsAny<Country>()))
                .ReturnsAsync(expectedCountry)
                .Callback(() => Console.WriteLine("AddCountry was called!"));

            // Act
            CountryResponse actualResponse = await _countryAdderService.AddCountryRequest(countryAddRequest);

            // Assert
            actualResponse.Should().NotBeNull();
            _countryRepositryContractMoq.Verify(
                repo => repo.AddCountry(It.Is<Country>(c => c.CountryName == uniqueCountryName)),
                Times.Once);
            _unitOfWorkMoq.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task AddCountryRequest_AddDuplicateCountry_ThrowsArgumentException()
        {
            // Arrange
            string existingCountryName = "Existing Country";

            // Create an existing country
            Country existingCountry = _fixture.Build<Country>()
                .With(c => c.CountryName, existingCountryName)
                .With(c => c.Persons, new List<Person>()) // Empty collection to avoid circular reference
                .Create();

            CountryAddRequest countryAddRequest = _fixture.Build<CountryAddRequest>()
                .With(c => c.CountryName, existingCountryName)
                .Create();

            // Setup mock to return existing country
            _countryRepositryContractMoq
                .Setup(repo => repo.GetCountryByName(existingCountryName))
                .ReturnsAsync(existingCountry);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(async () =>
                await _countryAdderService.AddCountryRequest(countryAddRequest));
        }

        #endregion

        #region GetAllCountriesRequest Tests

        [Fact]
        public async Task GetAllCountriesRequest_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            List<Country> emptyCountries = new List<Country>();

            _countryRepositryContractMoq
                .Setup(repo => repo.GetAllCountries())
                .ReturnsAsync(emptyCountries);

            // Act
            List<CountryResponse> countries = await _countryGetterService.Countries();

            // Assert
            countries.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllCountriesRequest_AddFewCountries_ReturnsAllCountries()
        {
            // Arrange
            List<Country> countries = new List<Country>();

            // Manually create countries to avoid circular reference issues
            for (int i = 0; i < 3; i++)
            {
                var country = _fixture.Build<Country>()
                    .With(c => c.CountryId, Guid.NewGuid())
                    .With(c => c.CountryName, $"Country_{Guid.NewGuid()}")
                    .With(c => c.Persons, new List<Person>()) // Empty collection to avoid circular reference
                    .Create();
                countries.Add(country);
            }

            List<CountryResponse> expectedCountries = countries
                .Select(c => c.ConvertToDto())
                .ToList();

            _countryRepositryContractMoq
                .Setup(repo => repo.GetAllCountries())
                .ReturnsAsync(countries);

            // Act
            List<CountryResponse> actualCountries = await _countryGetterService.Countries();

            // Assert
            actualCountries.Should().BeEquivalentTo(expectedCountries);
            actualCountries.Count.Should().Be(3);
        }

        #endregion

        #region GetCountryById Tests

        [Fact]
        public async Task GetCountryByCountryId_NullValue_ReturnsNull()
        {
            // Arrange
            Guid? cid = null;

            // Act
            CountryResponse? countryResponse = await _countryGetterService.GetCountryByCountryId(cid);

            // Assert
            countryResponse.Should().BeNull();
        }

        [Fact]
        public async Task GetCountryByCountryId_InvalidGuid_ReturnsNull()
        {
            // Arrange
            Guid invalidId = Guid.NewGuid();

            _countryRepositryContractMoq
                .Setup(repo => repo.GetCountryById(invalidId))
                .ReturnsAsync((Country?)null);

            // Act
            CountryResponse? countryResponse = await _countryGetterService.GetCountryByCountryId(invalidId);

            // Assert
            countryResponse.Should().BeNull();
        }

        [Fact]
        public async Task GetCountryByCountryId_ValidateCountryId_ReturnsCorrectCountry()
        {
            // Arrange
            Country country = _fixture.Build<Country>()
                .With(c => c.CountryId, Guid.NewGuid())
                .With(c => c.CountryName, "Test Country")
                .With(c => c.Persons, new List<Person>()) // Empty collection to avoid circular reference
                .Create();

            CountryResponse expectedResponse = country.ConvertToDto();

            _countryRepositryContractMoq
                .Setup(repo => repo.GetCountryById(country.CountryId))
                .ReturnsAsync(country);

            // Act
            CountryResponse? actualResponse = await _countryGetterService.GetCountryByCountryId(country.CountryId);

            // Assert
            actualResponse.Should().NotBeNull();
            actualResponse.Should().BeEquivalentTo(expectedResponse);
        }

        #endregion
    }
}