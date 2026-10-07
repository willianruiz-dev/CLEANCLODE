using Microsoft.EntityFrameworkCore;
using LocalDataBase.Services;

namespace LocalDataBase
{
    public static class DB_TransactionService
    {
        private static readonly HospitalApiService _apiService = new HospitalApiService();
        private static bool _sqliteInitialized = false;
        
        public static string LastError { get; private set; } = "";

        static DB_TransactionService()
        {
            InitializeSqlite();
        }

        private static void InitializeSqlite()
        {
            if (_sqliteInitialized) return;
            
            try
            {
                LocalSqliteContext.EnsureCreated();
                _sqliteInitialized = true;
                LastError = $"[{DateTime.Now:HH:mm:ss}] SQLite inicializado correctamente";
                System.Diagnostics.Debug.WriteLine(LastError);
            }
            catch (Exception ex)
            {
                LastError = $"[{DateTime.Now:HH:mm:ss}] Error inicializando SQLite: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(LastError);
            }
        }

        public static async Task<List<DB_Transaction>> GetAll() 
        {
            return new List<DB_Transaction>();
        }

        public static async Task<List<DB_Transaction>> GetByState(int state)
        {
            return new List<DB_Transaction>();
        }

        public static async Task<List<DB_Transaction>> GetByReference(string reference) 
        {
            return new List<DB_Transaction>();
        }

        public static async Task<List<DB_Transaction>> GetByDocument(string document)
        {
            try
            {
                var transactionsDto = await _apiService.GetTransactionsByDocumentAsync(document);
                var transactions = new List<DB_Transaction>();

                foreach (var dto in transactionsDto)
                {
                    var transaction = new DB_Transaction
                    {
                        TransactionId = dto.TransactionId,
                        IdApi = dto.IdApi,
                        Document = dto.Document,
                        Reference = dto.Reference,
                        Product = dto.Product,
                        TotalAmount = dto.TotalAmount,
                        RealAmount = dto.RealAmount,
                        IncomeAmount = dto.IncomeAmount,
                        ReturnAmount = dto.ReturnAmount,
                        Description = dto.Description,
                        IdStateTransaction = dto.IdStateTransaction,
                        StateTransaction = dto.StateTransaction,
                        DateCreated = dto.DateCreated,
                        DateUpdated = dto.DateUpdated
                    };
                    transactions.Add(transaction);
                }

                return transactions;
            }
            catch (Exception)
            {
                return new List<DB_Transaction>();
            }
        }

        public static async Task<bool> Create(DB_Transaction transaction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DB_TransactionService] Creando transacción en SQLite:");
                System.Diagnostics.Debug.WriteLine($"  IncomeAmount: {transaction.IncomeAmount}");
                System.Diagnostics.Debug.WriteLine($"  ReturnAmount: {transaction.ReturnAmount}");
                System.Diagnostics.Debug.WriteLine($"  Description: '{transaction.Description}'");
                System.Diagnostics.Debug.WriteLine($"  IdStateTransaction: {transaction.IdStateTransaction}");
                System.Diagnostics.Debug.WriteLine($"  StateTransaction: '{transaction.StateTransaction}'");

                await SaveToSqlite(transaction);

                return true;
            }
            catch (Exception ex)
            {
                LastError = $"[{DateTime.Now:HH:mm:ss}] Create Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(LastError);
                return false;
            }
        }

        public static async Task<bool> Update(DB_Transaction transaction)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DB_TransactionService] Actualizando transacción en SQLite: ID={transaction.TransactionId}");
                
                await SaveToSqlite(transaction);

                return true;
            }
            catch (Exception ex)
            {
                LastError = $"[{DateTime.Now:HH:mm:ss}] Update Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(LastError);
                return false;
            }
        }

        public static async Task<bool> CreateDetail(DB_TransactionDetail transactionDetail)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DB_TransactionService] Guardando detalle en SQLite: ID={transactionDetail.TranDetailId}");
                
                await SaveDetailToSqlite(transactionDetail);

                return true;
            }
            catch (Exception ex)
            {
                LastError = $"[{DateTime.Now:HH:mm:ss}] CreateDetail Error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(LastError);
                return false;
            }
        }

        private static async Task SaveToSqlite(DB_Transaction transaction)
        {
            try
            {
                using var context = new LocalSqliteContext();
                
                var existing = await context.DB_Transactions
                    .FirstOrDefaultAsync(t => t.IdApi == transaction.IdApi);

                if (existing != null)
                {
                    existing.IncomeAmount = transaction.IncomeAmount;
                    existing.ReturnAmount = transaction.ReturnAmount;
                    existing.Description = transaction.Description;
                    existing.IdStateTransaction = transaction.IdStateTransaction;
                    existing.StateTransaction = transaction.StateTransaction;
                    existing.DateUpdated = DateTime.Now;
                }
                else
                {
                    transaction.DateCreated = DateTime.Now;
                    transaction.DateUpdated = DateTime.Now;
                    context.DB_Transactions.Add(transaction);
                }

                await context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"[DB_TransactionService] Transacción guardada en SQLite: ID={transaction.TransactionId}, IdApi={transaction.IdApi}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB_TransactionService] Error guardando en SQLite: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DB_TransactionService] StackTrace: {ex.StackTrace}");
            }
        }

        private static async Task SaveDetailToSqlite(DB_TransactionDetail detail)
        {
            try
            {
                using var context = new LocalSqliteContext();
                
                var existing = await context.DB_TransactionDetails
                    .FirstOrDefaultAsync(t => t.IdApi == detail.IdApi);

                if (existing == null)
                {
                    detail.DateCreated = DateTime.Now;
                    context.DB_TransactionDetails.Add(detail);
                    await context.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"[DB_TransactionService] Detalle guardado en SQLite: ID={detail.TranDetailId}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB_TransactionService] Error guardando detalle en SQLite: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DB_TransactionService] StackTrace: {ex.StackTrace}");
            }
        }
    }
}
