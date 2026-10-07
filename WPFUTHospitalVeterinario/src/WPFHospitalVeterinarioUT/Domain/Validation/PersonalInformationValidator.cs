using System.Text.RegularExpressions;

namespace Domain.Validation
{
    /// <summary>
    /// Centraliza las reglas de entrada del formulario de información personal.
    /// No realiza navegación ni muestra UI, por lo que las reglas pueden probarse de forma aislada.
    /// </summary>
    public static class PersonalInformationValidator
    {
        /// <summary>Correo con forma usuario@dominio.tld.</summary>
        private static readonly Regex EmailPattern = new(
            @"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(250));

        /// <summary>Celular de Colombia: exactamente 10 dígitos y comienza por 3.</summary>
        private static readonly Regex MobilePattern = new(
            @"^3\d{9}$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Nombres y apellidos: letras (incluye tildes y ñ), espacios, apóstrofo, guion y punto.
        /// No admite números.
        /// </summary>
        private static readonly Regex NamePattern = new(
            @"^[\p{L}][\p{L}\s'\-\.]*$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(250));

        private const int MaxDocumentLength = 15;
        private const int MaxNameLength = 60;
        private const int MaxEmailLength = 254;

        public static string? GetValidationError(
            bool hasDocumentType,
            string? document,
            string? firstName,
            string? lastName,
            string? mobile,
            string? email)
        {
            document = document?.Trim();
            firstName = firstName?.Trim();
            lastName = lastName?.Trim();
            mobile = mobile?.Trim();
            email = email?.Trim();

            if (!hasDocumentType)
                return "Por favor, seleccione su tipo de documento.";
            if (string.IsNullOrWhiteSpace(document))
                return "Por favor, ingrese su número de documento.";
            if (!IsNumeric(document) || document.Length > MaxDocumentLength)
                return "Por favor, ingrese un número de documento válido (solo números).";
            if (string.IsNullOrWhiteSpace(firstName))
                return "Por favor, ingrese sus nombres.";
            if (!IsValidName(firstName) || firstName.Length > MaxNameLength)
                return "Por favor, ingrese sus nombres sin números ni símbolos.";
            if (string.IsNullOrWhiteSpace(lastName))
                return "Por favor, ingrese sus apellidos.";
            if (!IsValidName(lastName) || lastName.Length > MaxNameLength)
                return "Por favor, ingrese sus apellidos sin números ni símbolos.";
            if (string.IsNullOrWhiteSpace(mobile))
                return "Por favor, ingrese su número de celular.";
            if (!MobilePattern.IsMatch(mobile))
                return "Por favor, ingrese un celular válido: 10 dígitos y debe comenzar por 3.";
            if (string.IsNullOrWhiteSpace(email) || email.Length > MaxEmailLength || !EmailPattern.IsMatch(email))
                return "Por favor, ingrese un correo electrónico válido.";

            return null;
        }

        /// <summary>Documento exclusivamente numérico.</summary>
        public static bool IsNumeric(string? value) =>
            !string.IsNullOrEmpty(value) && value.All(char.IsDigit);

        /// <summary>Documento válido: solo números y dentro del largo máximo.</summary>
        public static bool IsValidDocument(string? value)
        {
            var document = value?.Trim();
            return IsNumeric(document) && document!.Length <= MaxDocumentLength;
        }

        /// <summary>Nombre o apellido válido: sin números ni símbolos y dentro del largo máximo.</summary>
        public static bool IsValidNameField(string? value) =>
            IsValidName(value) && value!.Trim().Length <= MaxNameLength;

        /// <summary>Celular de Colombia válido: exactamente 10 dígitos que comienzan por 3.</summary>
        public static bool IsValidMobile(string? value) =>
            MobilePattern.IsMatch(value?.Trim() ?? string.Empty);

        /// <summary>Correo válido: con forma usuario@dominio.tld y dentro del largo máximo.</summary>
        public static bool IsValidEmail(string? value)
        {
            var email = value?.Trim();
            return !string.IsNullOrWhiteSpace(email) && email!.Length <= MaxEmailLength && EmailPattern.IsMatch(email);
        }

        // Estas reglas por campo son las mismas que usa GetValidationError, en un solo lugar: así el
        // asterisco del formulario y el mensaje del modal nunca pueden decir cosas distintas.

        /// <summary>Nombre o apellido sin números.</summary>
        public static bool IsValidName(string? value) =>
            !string.IsNullOrWhiteSpace(value) &&
            NamePattern.IsMatch(value) &&
            value.Any(char.IsLetter);
    }
}
