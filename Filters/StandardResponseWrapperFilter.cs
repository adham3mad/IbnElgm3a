using IbnElgm3a.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

namespace IbnElgm3a.Filters
{
    public class StandardResponseWrapperFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // ModelState invalid logic handles 400 Bad Request
            if (!context.ModelState.IsValid)
            {
                var error = context.ModelState.FirstOrDefault(x => x.Value?.Errors.Count > 0);
                var fieldName = error.Key;
                var errorMessage = error.Value?.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation error";

                var response = ApiResponse<object>.CreateError(
                    code: "BAD_REQUEST",
                    message: errorMessage,
                    field: fieldName
                );

                if (context.HttpContext.Request.Headers.TryGetValue("X-Request-ID", out var requestId))
                {
                    response.Meta.RequestId = requestId.ToString();
                }

                context.Result = new BadRequestObjectResult(response);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
                return; // Let the GlobalExceptionHandler handle exceptions

            // If the controller returns Ok(data), CreatedAtRoute(data), etc.
            if (context.Result is ObjectResult objectResult)
            {
                // Prevent double-wrapping if the controller already returned an ApiResponse
                if (objectResult.Value?.GetType().IsGenericType == true && 
                    objectResult.Value.GetType().GetGenericTypeDefinition() == typeof(ApiResponse<>))
                {
                    return; 
                }

                var standardResponse = ApiResponse<object>.CreateSuccess(objectResult.Value);
                if (context.HttpContext.Request.Headers.TryGetValue("X-Request-ID", out var reqId))
                {
                    standardResponse.Meta.RequestId = reqId.ToString();
                }

                objectResult.Value = standardResponse;
            }
            // If the controller returns NoContent() or just an OkResult without data
            else if (context.Result is StatusCodeResult statusCodeResult && statusCodeResult.StatusCode >= 200 && statusCodeResult.StatusCode < 300)
            {
                if (statusCodeResult.StatusCode == 204)
                    return; // Allow 204 No Content to pass directly

                var standardResponse = ApiResponse<object>.CreateSuccess(null);
                if (context.HttpContext.Request.Headers.TryGetValue("X-Request-ID", out var reqId))
                {
                    standardResponse.Meta.RequestId = reqId.ToString();
                }
                
                context.Result = new ObjectResult(standardResponse) { StatusCode = statusCodeResult.StatusCode };
            }
        }
    }
}
