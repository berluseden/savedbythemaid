namespace JempSoft.Core.Common
{
    public class Error
    {
        public string Code { get; }
        public string Description { get; }

        public Error(string code, string description)
        {
            Code = code;
            Description = description;
        }

        public static readonly Error None = new Error(string.Empty, string.Empty);
    }
}
