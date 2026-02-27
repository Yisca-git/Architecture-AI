using DTOs;
using Entities;

namespace Services
{
    public interface IDressService
    {
        Task<bool> IsExistsDressById(int id);
        bool checkDate(DateOnly date);
        bool checkPrice(int price);
        Task DeleteDress(int id);
        Task<int> GetPriceById(int id);
        Task<DressDTO> GetDressByModelIdAndSize(int modelId, string size);
        Task<List<DressDTO>> GetDressesByModelId(int modelId);
        Task<int> GetCountByModelIdAndSizeForDate(int modelId, string size, DateOnly date);
        Task<DressDTO> GetDressById(int id);
        Task<bool> IsDressAvailable(int id, DateOnly date);
        Task<List<string>> GetSizesByModelId(int modelId);
        Task<DressDTO> AddDress(NewDressDTO newDress);
        Task UpdateDress(int id, NewDressDTO updateDress);
    }
}