using CVR.Newtonsoft.Json;

namespace CVR.CCKEditor.API
{
    public class ApiResponse<T>
    {
        public string Message { get; set; }
        public T Data { get; set; }
        
        public string Type { get; set; }
        public  System.Net.HttpStatusCode Status { get; set; }
        
        public ApiResponse(string message, T data, string type, System.Net.HttpStatusCode status)
        {
            Message = message;
            Data = data;
            Type = type;
            Status = status;
        }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}