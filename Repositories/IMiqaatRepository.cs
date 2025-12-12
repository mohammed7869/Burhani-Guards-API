using BurhaniGuards.Api.BusinessModel;

namespace BurhaniGuards.Api.Repositories;

public interface IMiqaatRepository
{
    Task<long> Add(MiqaatModel model);
    Task<List<MiqaatModel>> GetAll();
    Task<MiqaatModel?> GetById(long id);
    Task Update(MiqaatModel model);
    Task Delete(long id);
    Task<List<MiqaatModel>> GetByCaptainName(string captainName);
}

