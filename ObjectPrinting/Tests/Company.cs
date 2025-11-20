using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectPrinting.Tests
{
    public class Company
    {
        public string Name { get; set; }
        public List<Person> Employees { get; set; }
        public decimal Budget { get; set; }
    }
}
