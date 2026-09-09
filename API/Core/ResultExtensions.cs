using Microsoft.AspNetCore.Mvc;

namespace API.Core
{
    public static class ResultExtensions
    {
        public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
        {
            if (result.IsSuccess)
                return controller.Ok(result.Value);

            return ToProblem(result.Error!, controller);
        }

        public static IActionResult ToActionResult(this Result result, ControllerBase controller)
        {
            if (result.IsSuccess)
                return controller.NoContent();

            return ToProblem(result.Error!, controller);
        }

        private static IActionResult ToProblem(Error error, ControllerBase controller)
        {
            return controller.StatusCode(error.StatusCode, new ProblemDetails
            {
                Status = error.StatusCode,
                Title = error.Message,
                Type = $"urn:api:errors:{error.Code}",
                Extensions =
                {
                    ["traceId"] = controller.HttpContext.TraceIdentifier,
                    ["timestamp"] = DateTime.UtcNow
                }
            });
        }
    }
}
