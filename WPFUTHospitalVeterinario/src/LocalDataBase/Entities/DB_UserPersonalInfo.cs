using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalDataBase
{
    public class DB_UserPersonalInfo
    {
        [Key]
        [StringLength(50)]
        public string Document { get; set; }
        
        [Required]
        [StringLength(50)]
        public string DocumentType { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Name { get; set; }
        
        [Required]
        [StringLength(255)]
        public string LastName { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Mobile { get; set; }
        
        [Required]
        [StringLength(255)]
        public string Email { get; set; }
    }
}
