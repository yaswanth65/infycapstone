using EventManagementSystemServiceLayer.DTOs;
using System.Net;
using System.Text.Json;

namespace EventManagementSystemServiceLayer.Middleware
{
   public class GlobalExceptionMiddleware
   {
       private readonly RequestDelegate _next;
       private readonly ILogger<GlobalExceptionMiddleware> _logger;

       public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
       {
           _next = next ?? throw new ArgumentNullException(nameof(next));
           _logger = logger ?? throw new ArgumentNullException(nameof(logger));
       }

       public async Task InvokeAsync(HttpContext context)
       {
           try
           {
               await _next(context);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
               await HandleExceptionAsync(context);
           }
       }

       private static async Task HandleExceptionAsync(HttpContext context)
       {
           context.Response.ContentType = "application/json";
           context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

           var response = new ApiResponse<object>(
               false,
               (int)HttpStatusCode.InternalServerError,
               "An unexpected error occurred. Please try again later.");

           var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
           await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
       }
   }

   public static class GlobalExceptionMiddlewareExtensions
   {
       public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder app)
       {
           return app.UseMiddleware<GlobalExceptionMiddleware>();
       }
   }
}
