
using Entities;
using Microsoft.EntityFrameworkCore;
using RepositryContracts;
using System.Threading.Tasks;

namespace Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDBContext _db;

        public UnitOfWork(AppDBContext db)
        {
            _db = db;
        }




        public async Task<int> SaveChangesAsync()
        {
            return await _db.SaveChangesAsync();
        }
    }
}

