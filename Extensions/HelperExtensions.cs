namespace DopamineDetoxFunction.Extensions
{
    public static class HelperExtensions
    {

        public static string FormatErrorMessages(this List<string> errorMessages)
        {
            if (errorMessages == null || errorMessages.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(Environment.NewLine, errorMessages.Select((error, index) => $"{index + 1}. {error}"));
        }
    }
}
