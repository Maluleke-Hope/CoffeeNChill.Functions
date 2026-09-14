using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace ST10467898CLDV6212POE.Functions;

public class RouteTestFunctions
{
    [Function("RouteTest")]
    public HttpResponseData RouteTest(
        [HttpTrigger(
            AuthorizationLevel.Anonymous,
            "get",
            Route = "test/{id}")]
        HttpRequestData req,
        string id)
    {
        HttpResponseData response =
            req.CreateResponse(HttpStatusCode.OK);

        response.WriteString($"Route works. ID = {id}");

        return response;
    }
}