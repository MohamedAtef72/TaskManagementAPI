using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_Management_Api.Application.Interfaces
{
    public interface IRoleSeederService
    {
        Task SeedRolesAndAdminAsync();

    }
}
