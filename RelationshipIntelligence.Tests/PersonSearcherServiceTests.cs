using ContactsManger.Core.Domain.Entities;
using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ContactsManger.Core.DTOs.PersonDTOs;
using Servicess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class PersonSearcherServiceTests
    {
        private readonly Mock<PersonRepositryContract> _repoMock = new();

        private PersonSearcherService Service() => new(
            _repoMock.Object,
            Mock.Of<ILogger<PersonSearcherService>>());

        private void ArrangePeople(params Person[] people)
        {
            _repoMock.Setup(r => r.GetFilteredPersonsPaged(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Expression<Func<Person, bool>>>()))
                .ReturnsAsync((int pageNumber, int pageSize, Expression<Func<Person, bool>> predicate) =>
                {
                    var filtered = people.AsQueryable().Where(predicate).ToList();
                    return (filtered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(), filtered.Count);
                });
        }

        private static Person PersonWithCircles(string name, params string[] circles)
        {
            var person = new Person
            {
                PersonId = Guid.NewGuid(),
                Name = name,
                ApplicationUserId = Guid.NewGuid()
            };
            foreach (var circle in circles)
                person.Circles.Add(new Circle { CircleId = Guid.NewGuid(), Name = circle });
            return person;
        }

        [Fact]
        public async Task CompositeFilter_CircleName_MatchesExactCompany()
        {
            ArrangePeople(PersonWithCircles("Salma El-Sayed", "Proceedit"));

            var result = await Service().SearchPersonsByCompositeFilter(
                new PersonCompositeFilter { CircleName = "Proceedit" }, 1, 10);

            result.Items.Should().ContainSingle(i => i.Name == "Salma El-Sayed");
        }

        [Fact]
        public async Task Batched_NameSearch_IsCaseInsensitive()
        {
            ArrangePeople(PersonWithCircles("Mohamed Farouk", "Proceedit"));

            var result = await Service().SearchPersonsBy_Batched("mohamed", "Name", 1, 10);

            result.Items.Should().ContainSingle(i => i.Name == "Mohamed Farouk");
        }

        [Fact]
        public async Task CompositeFilter_CircleName_IsCaseInsensitive()
        {
            ArrangePeople(PersonWithCircles("Salma El-Sayed", "Proceedit"));

            var result = await Service().SearchPersonsByCompositeFilter(
                new PersonCompositeFilter { CircleName = "proceedit" }, 1, 10);

            result.Items.Should().ContainSingle(i => i.Name == "Salma El-Sayed");
        }

        [Fact]
        public async Task CompositeFilter_CircleName_DoesNotSubstringMatch()
        {
            // "Pro" must not match "Proceedit" — raw-substring guard.
            ArrangePeople(
                PersonWithCircles("Salma El-Sayed", "Proceedit"),
                PersonWithCircles("Karim Naguib", "Proceedit Labs"));

            var result = await Service().SearchPersonsByCompositeFilter(
                new PersonCompositeFilter { CircleName = "Proceedit" }, 1, 10);

            result.Items.Should().ContainSingle(i => i.Name == "Salma El-Sayed");
        }

        [Fact]
        public async Task CompositeFilter_CircleName_MultiOrgPersonFoundUnderBoth()
        {
            ArrangePeople(PersonWithCircles("Omar Khalil", "Proceedit", "Community"));

            var first = await Service().SearchPersonsByCompositeFilter(
                new PersonCompositeFilter { CircleName = "Proceedit" }, 1, 10);
            var second = await Service().SearchPersonsByCompositeFilter(
                new PersonCompositeFilter { CircleName = "Community" }, 1, 10);

            first.Items.Should().ContainSingle(i => i.Name == "Omar Khalil");
            second.Items.Should().ContainSingle(i => i.Name == "Omar Khalil");
        }
    }
}
