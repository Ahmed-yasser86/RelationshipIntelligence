namespace ServiceContracts
{
    public interface IPersonDeleterService
    {
        Task<bool> DeletePersonByPersonId(Guid? personId);
    }
}