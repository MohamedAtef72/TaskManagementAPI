using Microsoft.AspNetCore.Identity;
using Task_Management_API.DTO;
using Task_Management_API.Models;

namespace Task_Management_API.Interfaces
{
    public interface IMainRepository<T> where T : class
    {
        // Generic CRUD Operations
        Task<T> GetByIdAsync(int id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task<bool> DeleteByIdAsync(int id);
        Task<bool> ExistsAsync(int id);
        Task SaveAsync();
    }
}