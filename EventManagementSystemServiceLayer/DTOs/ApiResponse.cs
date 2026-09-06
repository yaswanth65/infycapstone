namespace EventManagementSystemServiceLayer.DTOs
{
   public class ApiResponse<T>
   {
       public bool Success { get; set; }
       public int StatusCode { get; set; }
       public string Message { get; set; } = null!;
       public T? Data { get; set; }
       public Dictionary<string, List<string>>? Errors { get; set; }

       public ApiResponse() { }

       public ApiResponse(bool success, int statusCode, string message, T? data = default, Dictionary<string, List<string>>? errors = null)
       {
           Success = success;
           StatusCode = statusCode;
           Message = message;
           Data = data;
           Errors = errors;
       }
   }
}
