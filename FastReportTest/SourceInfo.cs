using System;
using System.Collections.Generic;
using System.Text;

namespace FastReportTest
{
    class SourceInfo
    {
        public SourceInfo()
        {
            Columns = new Dictionary<string, string>();
        }
        public string TableName { get; set; }
        public Dictionary<string,string> Columns { get; set; }
    }
}
