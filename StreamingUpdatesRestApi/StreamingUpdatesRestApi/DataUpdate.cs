﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamingUpdatesRestApi
{
    public class DataUpdate
    {
        public IEnumerable<Update> data { get; set; }
    }

    public class Update
    {
        public string resourceId { get; set; }
        public string operation { get; set; }
        public IEnumerable<SdsSimpleType> events { get; set; }
    }
}
