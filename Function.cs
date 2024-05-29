using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Net;
using System.ComponentModel;

namespace RelayFun
{
    public class Function
    {
        private const string pk0 = "/pk0";

        private readonly CosmosClient Client;
        private readonly Database Database;
        private readonly Container Sessions;

        private readonly ILogger<Function> _logger;
        public Function(ILogger<Function> logger)
        {
            var cs = "cs";
            Client = new CosmosClient(cs);

            Database = Client.CreateDatabaseIfNotExistsAsync(nameof(Database)).Result;
            Sessions = Database.CreateContainerIfNotExistsAsync(nameof(Sessions), pk0).Result;

            _logger = logger;
        }

        [Function("HostLobby")]
        public async Task<IActionResult> HostLobby([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            _logger.LogInformation("Received request to host lobby");

            IPAddress? ip = req.HttpContext.Connection.RemoteIpAddress;
            if (ip == null)
                return new BadRequestObjectResult("IP is invalid");

            string? lobbyName = nameof(lobbyName);
            bool sucess = QueryParameter(req, ref lobbyName);
            if (!sucess)
                return new BadRequestObjectResult(lobbyName);

            Endpoint ep = new(ip, lobbyName);
            var readRes = await Sessions.UpsertItemAsync(ep);
            if (readRes.StatusCode == (HttpStatusCode)201)
            {
                return new OkObjectResult("Lobby was created");
            }
            else if (readRes.StatusCode == (HttpStatusCode)200)
            {
                return new BadRequestObjectResult("Lobby is already created");
            }
            else
            {
                return new BadRequestObjectResult("Unable to complete request");
            }
        }

        [Function("JoinLobby")]
        public async Task<IActionResult> JoinLobby([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
        {
            _logger.LogInformation("Received request to join lobby");

            IPAddress? ip = req.HttpContext.Connection.RemoteIpAddress;
            if (ip == null)
                return new BadRequestObjectResult("IP is invalid");

            string? lid = nameof(lid);
            bool success = QueryParameter(req, ref lid);
            if (!success)
                return new BadRequestObjectResult(lid);

            string? playerName = nameof(playerName);
            success = QueryParameter(req, ref playerName);
            if (!success)
                return new BadRequestObjectResult(playerName);

            var (exists, ep) = await QueryLobby(lid);
            if (exists)
            {
                // add player to lobby
                return new OkObjectResult("Joined lobby");
            }
            else
            {
                return new BadRequestObjectResult("Unable to find lobby");
            }
        }

        /// <summary>
        /// Tries to query the parameter in req
        /// </summary>
        /// <param name="req">the request to query</param>
        /// <param name="parameter">the name of the parameter</param>
        /// <returns>true if parameter was found, the parameter value will be in parameter, if false, the error will be in parameter</returns>
        private static bool QueryParameter(HttpRequest req, ref string? parameter)
        {
            if (parameter == null)
            {
                parameter = "Invalid parameter was provided";
                return false;
            }

            var og = parameter;
            parameter = req.Query[parameter];
            if (string.IsNullOrWhiteSpace(parameter))
            {
                parameter = $"Parameter '{og}' couldn't be found";
                return false;
            }

            return true;
        }

        public async Task<(bool exists, Endpoint? ep)> QueryLobby(string id)
        {
            try
            {
                // TODO: Unable to find item
                var res = await Sessions.ReadItemAsync<Endpoint>(id, new(pk0));
                return new(res.StatusCode == HttpStatusCode.OK, res.Resource);
            }
            catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return new(false, null);
            }
            catch
            {
                throw;
            }
        }

    }
}