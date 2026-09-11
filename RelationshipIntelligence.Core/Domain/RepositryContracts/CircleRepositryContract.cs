using ContactsManger.Core.Domain.Entities;
using System;

namespace RepositryContracts
{
    public interface CircleRepositryContract
    {
        Task<Circle> AddCircle(Circle circle);

        Task<Circle>? GetCircleById(Guid? id);

        Task<Circle>? GetCircleByName(string name);

        Task<IEnumerable<Circle>> GetAllCircles();

        Task<IEnumerable<Circle>> GetCirclesByNames(IEnumerable<string> names);
    }
}