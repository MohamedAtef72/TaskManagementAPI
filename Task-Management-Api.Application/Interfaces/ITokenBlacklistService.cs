using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_Management_Api.Application.Interfaces
{
    public interface ITokenBlacklistService
    {
        Task AddTokenToBlacklistAsync(string token, DateTime expiresAt);
        Task<bool> IsTokenBlacklistedAsync(string token);
    }
}
