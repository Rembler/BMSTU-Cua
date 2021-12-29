using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cua.Models
{
    public class HubUser
    {
        [Key]
        public string Name { get; set; }
        public virtual ICollection<HubGroup> HubGroups { get; set; } 
    }

    public class HubGroup
    {
        [Key]
        public string Name { get; set; }
        public virtual ICollection<HubUser> HubUsers { get; set; }
    }
}    