using ContactsManger.Core.Domain.Entities;
using Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RepositryContracts;
using ServiceContracts;
using Servicess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace CRUDTests
{
    public class OrganizationServiceTests
    {
        private readonly Mock<CircleRepositryContract> _circlesMock = new();
        private readonly Mock<IUnitOfWork> _uowMock = new();

        private OrganizationService Service() => new(
            _circlesMock.Object,
            _uowMock.Object,
            Mock.Of<ILogger<OrganizationService>>());

        [Fact]
        public async Task GetAllAsync_ReturnsMembersOrderedByName()
        {
            _circlesMock.Setup(r => r.GetCirclesWithMembers()).ReturnsAsync(new List<Circle>
            {
                new() { CircleId = Guid.NewGuid(), Name = "Proceedit", People = new List<Person> { new(), new() } },
                new() { CircleId = Guid.NewGuid(), Name = "Family", People = new List<Person>() }
            }.AsEnumerable());

            var result = await Service().GetAllAsync();

            result.Should().HaveCount(2);
            result.First().Name.Should().Be("Family");
            result.Last().MemberCount.Should().Be(2);
        }

        [Fact]
        public async Task CreateAsync_DuplicateName_Throws()
        {
            _circlesMock.Setup(r => r.GetCircleByName("proceedit"))
                .ReturnsAsync(new Circle { CircleId = Guid.NewGuid(), Name = "Proceedit" });

            await Assert.ThrowsAsync<ArgumentException>(() => Service().CreateAsync("  proceedit  "));
        }

        [Fact]
        public async Task CreateAsync_BlankName_Throws()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => Service().CreateAsync("   "));
        }

        [Fact]
        public async Task RenameAsync_UnknownId_ThrowsNotFound()
        {
            _circlesMock.Setup(r => r.GetCircleById(It.IsAny<Guid?>())).ReturnsAsync((Circle?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => Service().RenameAsync(Guid.NewGuid(), "New"));
        }

        [Fact]
        public async Task DeleteAsync_WithMembers_Throws()
        {
            var id = Guid.NewGuid();
            _circlesMock.Setup(r => r.GetCirclesWithMembers()).ReturnsAsync(new List<Circle>
            {
                new() { CircleId = id, Name = "Proceedit", People = new List<Person> { new() } }
            }.AsEnumerable());

            await Assert.ThrowsAsync<InvalidOperationException>(() => Service().DeleteAsync(id));
            _circlesMock.Verify(r => r.RemoveCircle(It.IsAny<Circle>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_Empty_RemovesAndSaves()
        {
            var id = Guid.NewGuid();
            var circle = new Circle { CircleId = id, Name = "Old", People = new List<Person>() };
            _circlesMock.Setup(r => r.GetCirclesWithMembers()).ReturnsAsync(new List<Circle> { circle }.AsEnumerable());

            await Service().DeleteAsync(id);

            _circlesMock.Verify(r => r.RemoveCircle(circle), Times.Once);
            _uowMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
