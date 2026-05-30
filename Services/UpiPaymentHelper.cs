namespace WorkerBookingSystem.Services
{
    public static class UpiPaymentHelper
    {
        public static string NormalizeIndiaPhone(string phoneNumber)
        {
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
            if (digits.Length == 10)
            {
                return "+91" + digits;
            }

            if (digits.Length == 12 && digits.StartsWith("91"))
            {
                return "+" + digits;
            }

            if (phoneNumber.TrimStart().StartsWith('+'))
            {
                return "+" + digits;
            }

            return phoneNumber.Trim();
        }

        public static string BuildUpiPayUri(string merchantVpa, string merchantName, decimal amount, string note)
        {
            var encodedVpa = Uri.EscapeDataString(merchantVpa);
            var encodedName = Uri.EscapeDataString(merchantName);
            var encodedNote = Uri.EscapeDataString(note);
            return $"upi://pay?pa={encodedVpa}&pn={encodedName}&am={amount:F2}&cu=INR&tn={encodedNote}";
        }

        public static string BuildQrCodeUrl(string upiUri, int size = 260)
        {
            return $"https://api.qrserver.com/v1/create-qr-code/?size={size}x{size}&data={Uri.EscapeDataString(upiUri)}";
        }
    }
}
