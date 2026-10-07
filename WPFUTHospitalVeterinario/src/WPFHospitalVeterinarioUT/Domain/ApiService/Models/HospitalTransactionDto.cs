namespace ApiService.Models
{
    /// <summary>
    /// Contrato publicado por WSHospitalVeterinarioUT para su base transaccional.
    /// Se mantiene separado de los DTO del Dashboard porque sus identificadores son distintos.
    /// </summary>
    public sealed class HospitalTransactionDto
    {
        public int TransactionId { get; set; }
        public string? IdApi { get; set; }
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
        public DateTime? DateCreated { get; set; }
        public DateTime? DateUpdated { get; set; }
    }
}
