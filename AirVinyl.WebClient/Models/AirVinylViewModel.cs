using AirVinyl.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirVinyl.WebClient.Models
{
    public class AirVinylViewModel
    {
        public IEnumerable<Person> People { get; set; }
        public Person Person { get; set; }
        public string AdditionalData { get; set; }

        public AirVinylViewModel()
        {
            People = new List<Person>();
        }
    }
}
