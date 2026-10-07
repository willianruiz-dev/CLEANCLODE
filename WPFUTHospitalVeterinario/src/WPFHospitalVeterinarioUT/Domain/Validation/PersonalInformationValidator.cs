using System.Text.RegularExpressions;

namespace Domain.Validation
{
    /// <summary>
    /// Centraliza las reglas de entrada del formulario de información personal.
    /// No realiza navegación ni muestra UI, por lo que las reglas pueden probarse de forma aislada.
    /// </summary>
    public static class PersonalInformationValidator
    {
        private static readonly Regex EmailPattern = new(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(250));

        public static string? GetValidationError(
            bool hasDocumentType,
            string? document,
            string? firstName,
            string? lastName,
            string? mobile,
            string? email)
        {
            if (!hasDocumentType)
                return "Por favor, seleccione su tipo de documento.";
            if (string.IsNullOrWhiteSpace(document))
                return "Por favor, ingrese su número de documento.";
            if (!IsNumeric(document))
                return "Por favor, ingrese un número de documento válido.";
            if (string.IsNullOrWhiteSpace(firstName))
                return "Por favor, ingrese sus nombres.";
            if (string.IsNullOrWhiteSpace(lastName))
                return "Por favor, ingrese sus apellidos.";
            if (string.IsNullOrWhiteSpace(mobile))
                return "Por favor, ingrese su número de celular.";
            if (!IsNumeric(mobile))
                return "Por favor, ingrese un número de celular válido.";
            if (string.IsNullOrWhiteSpace(email) || !EmailPattern.IsMatch(email))
                return "Por favor, ingrese un correo electrónico válido.";

            return null;
        }

        private static bool IsNumeric(string value) => value.All(char.IsDigit);
    }
}
