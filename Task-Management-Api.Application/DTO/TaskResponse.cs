using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_Management_Api.Application.DTO
{
    public class TaskResponse
    {
        public string Message { get; set; }
        public TaskInformation Task { get; set; }
    }
}
