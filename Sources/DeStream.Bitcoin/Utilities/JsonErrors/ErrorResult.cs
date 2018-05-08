using Microsoft.AspNetCore.Mvc;

namespace DeStream.Bitcoin.Utilities.JsonErrors
{
    public class ErrorResult : ObjectResult
    {
        public ErrorResult(int statusCode, ErrorResponse value) : base(value)
        {
            this.StatusCode = statusCode;
        }
    }
}
