// ────────────────────────────────────────────────────────────────────────────────
// ViewModel del flujo de pago. Se extrajo de PaymentUC.xaml.cs para que ese
// archivo (la vista + su lógica de pantalla) no siguiera creciendo.
// Ambas clases quedan en el mismo namespace, por lo que PaymentUC y el XAML las
// siguen resolviendo exactamente igual: el movimiento es transparente para el
// resto del proyecto.
// ────────────────────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Presentation.UserControls.Flows
{
    public class PaymentViewModel : INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler? PropertyChanged;


        #region Attributes
        private decimal _payAmount;

        private decimal _enteredAmount;

        private decimal _remainingAmount;

        private decimal _returnAmount;

        public decimal _dispensedAmount;

        public bool _isReturnSuccess;

        private List<Denomination> _denominations;


        public List<Denomination> Denominations
        {
            get { return _denominations; }
            set
            {
                _denominations = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Denominations)));
            }
        }

        public decimal PayAmount
        {
            get { return _payAmount; }
            set
            {
                if (_payAmount != value)
                {
                    _payAmount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PayAmount)));
                }
            }
        }


        public decimal EnteredAmount
        {
            get { return _enteredAmount; }
            set
            {
                if (_enteredAmount != value)
                {
                    _enteredAmount = value;
                    RemainingAmount = (EnteredAmount < PayAmount) ? PayAmount - EnteredAmount : 0;
                    ReturnAmount = (EnteredAmount > PayAmount) ? EnteredAmount - PayAmount : 0;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EnteredAmount)));
                }
            }
        }

        public decimal RemainingAmount
        {
            get { return _remainingAmount; }
            set
            {
                if (_remainingAmount != value)
                {
                    _remainingAmount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RemainingAmount)));
                }
            }
        }

        public decimal ReturnAmount
        {
            get { return _returnAmount; }
            set
            {
                if (_returnAmount != value)
                {
                    _returnAmount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReturnAmount)));
                }
            }
        }

        public decimal DispensedAmount
        {
            get { return _dispensedAmount; }
            set
            {
                if (_dispensedAmount != value)
                {
                    _dispensedAmount = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DispensedAmount)));
                }
            }
        }

        public bool IsPayCompleted
        {
            get { return _isReturnSuccess; }
            set
            {
                if (_isReturnSuccess != value)
                {
                    _isReturnSuccess = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPayCompleted)));
                }
            }
        }

        #endregion

        #region Methods
        public void RefreshAmountsList(int denomination, int quantity)
        {

            var itemDenomination = Denominations.Where(d => d.DenominationValue == denomination).FirstOrDefault();
            if (itemDenomination == null)
            {
                Denominations.Add(new Denomination
                {
                    DenominationValue = denomination,
                    Quantity = quantity,
                    TotalDenomAmount = denomination * quantity,
                });
                return;
            }

            itemDenomination.Quantity += quantity;
            itemDenomination.TotalDenomAmount = denomination * itemDenomination.Quantity;
        }

        #endregion
    }

    public class Denomination
    {
        public decimal DenominationValue { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalDenomAmount { get; set; }
    }
}
