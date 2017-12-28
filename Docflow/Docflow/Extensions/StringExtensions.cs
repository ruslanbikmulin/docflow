namespace Docflow.Extensions
{
    public static class StringExtensions
    {
        public static string AddQuotes(this string inputString)
        {
            return string.Format("\'{0}\'", inputString);
        }
    }
}