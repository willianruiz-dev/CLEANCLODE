namespace ApiService.Models
{
    public class TransactionDto : DtoCommon
    {
        public string? Document { get; set; }
        public string? Reference { get; set; }
        public string? Product { get; set; }
        public double TotalAmount { get; set; }
        public double RealAmount { get; set; }
        public double IncomeAmount { get; set; }
        public double ReturnAmount { get; set; }
        public string? Description { get; set; }
        public int IdStateTransaction { get; set; }
        public string? StateTransaction { get; set; }
        public int IdTypeTransaction { get; set; }
        public string? TypeTransaction { get; set; }
        public int IdTypePayment { get; set; }
        public string? TypePayment { get; set; }
        public int IdPayPad { get; set; }
        public string? PayPad { get; set; }


    }
}
