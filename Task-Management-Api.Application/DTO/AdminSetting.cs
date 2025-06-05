using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_Management_Api.Application.DTO
{
    public class AdminSetting
    {
        public List<string> AdminEmails { get; set; } = new();
        public string DefaultAdminPassword { get; set; } = "Admin@123456";
    }
}
