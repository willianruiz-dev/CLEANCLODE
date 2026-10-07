using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LocalDataBase
{
    public class DB_Transaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TransactionId { get; set; }
        
        [StringLength(50)]
        public string IdApi { get; set; } = "";
        
        [StringLength(255)]
        public string? Document { get; set; }
        
        [StringLength(255)]
        public string? Reference { get; set; }
        
        [StringLength(255)]
        public string? Product { get; set; }
        
        public decimal TotalAmount { get; set; }
        public decimal RealAmount { get; set; }
        public decimal IncomeAmount { get; set; }
        public decimal ReturnAmount { get; set; }
        
        public string? Description { get; set; }
        
        public int IdStateTransaction { get; set; }
        
        [StringLength(255)]
        public string? StateTransaction { get; set; }

        public int IdTypeTransaction { get; set; }
        
        [StringLength(255)]
        public string? TypeTransaction { get; set; }

        public int IdTypePayment { get; set; }
        
        [StringLength(255)]
        public string? TypePayment { get; set; }

        public int IdPayPad { get; set; }
        
        [StringLength(255)]
        public string? PayPad { get; set; }

        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set;}
    }
}
