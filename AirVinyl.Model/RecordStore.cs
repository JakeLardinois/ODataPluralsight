using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirVinyl.Model
{
    public class RecordStore
    {
        [Key]
        public int RecordStoreId { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; }

        public Address StoreAddress { get; set; }

        private ICollection<string> _tags { get; set; }
        public ICollection<string> Tags
        {
            get { return _tags; }
            set { _tags = value; }
        }

        public string TagsAsString
        {
            get
            {
                if (_tags == null || !_tags.Any())
                {
                    return "";
                }
                return _tags.Aggregate((a, b) => a + "," + b);
            }
            set
            {
                _tags = value.Split(',').ToList();
            }
        }

        public ICollection<Rating> Ratings { get; set; }

        public RecordStore()
        {
            StoreAddress = new Address();
            Ratings = new List<Rating>();
            Tags = new List<string>();
        }
    }
}
