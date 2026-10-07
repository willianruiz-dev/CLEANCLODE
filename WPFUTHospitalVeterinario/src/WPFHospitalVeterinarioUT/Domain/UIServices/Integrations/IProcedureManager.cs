using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.UIServices.Integrations
{
    public interface IPaymentProcessManager
    {
        public Task NotifyPay();
        public bool IsRoundTotal();

    }

    public interface IConsultReferencesManager
    {
        public Task ReadFromScanner(string scannerRead);
        public Task ReadFromManualInput(string inputData);

    }
    public class ProcedureException: Exception
    {
        public ProcedureException() { }

        public ProcedureException(string? message) : base(message)
        {
        }

        public ProcedureException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
