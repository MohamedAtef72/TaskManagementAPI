﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Task_Management_Api.Application.DTO
{
    public class ErrorResponse
    {
        public string Message { get; set; }
        public IEnumerable<string> Errors { get; set; } = new List<string>();
    }
}
