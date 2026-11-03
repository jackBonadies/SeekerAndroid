using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Soulseek;
using WebAPI.DTO;

namespace WebAPI.Controllers
{
    public class InterestsRequest
    {
        public List<string> Likes { get; set; }
        public List<string> Hates { get; set; }
    }

    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class RecommendationsController : ControllerBase
    {
        public RecommendationsController(ISoulseekClient client)
        {
            Client = client;
        }

        private ISoulseekClient Client { get; }

        [HttpPost("interests")]
        public async Task<IActionResult> AddInterests([FromBody] InterestsRequest interests)
        {
            Console.WriteLine(JsonConvert.SerializeObject(interests));

            foreach (var like in interests?.Likes ?? new())
            {
                await Client.AddLikedInterestAsync(like);
                Console.WriteLine($"Added like: {like}");
            }

            foreach (var hate in interests?.Hates ?? new())
            {
                await Client.AddHatedInterestAsync(hate);
                Console.WriteLine($"Added hate: {hate}");
            }

            return StatusCode(StatusCodes.Status201Created);
        }

        [HttpDelete("interests")]
        public async Task<IActionResult> RemoveInterests([FromBody] InterestsRequest interests)
        {
            foreach (var like in interests?.Likes ?? new())
            {
                await Client.RemoveLikedInterestAsync(like);
                Console.WriteLine($"Removed like: {like}");
            }

            foreach (var hate in interests?.Hates ?? new())
            {
                await Client.RemoveHatedInterestAsync(hate);
                Console.WriteLine($"Removed hate: {hate}");
            }

            return StatusCode(StatusCodes.Status204NoContent);
        }

        [HttpGet("interests")]
        public async Task<IActionResult> GetInterests()
        {
            return Ok(await Client.GetInterestsAsync());
        }

        [HttpGet("interests/{username}")]
        public async Task<IActionResult> GetInterests([FromRoute] string username)
        {
            return Ok(await Client.GetUserInterestsAsync(username));
        }

        [HttpGet("similar")]
        public async Task<IActionResult> GetSimilarUsers()
        {
            return Ok(await Client.GetSimilarUsersAsync());
        }

        [HttpGet("similar/{interest}")]
        public async Task<IActionResult> GetSimilarUsers([FromRoute] string interest)
        {
            return Ok(await Client.GetInterestSimilarUsersAsync(interest));
        }

        [HttpGet("personal")]
        public async Task<IActionResult> GetPersonalRecommendations()
        {
            return Ok(await Client.GetPersonalRecommendationsAsync());
        }

        [HttpGet("global")]
        public async Task<IActionResult> GetGlobalRecommendations()
        {
            return Ok(await Client.GetGlobalRecommendationsAsync());
        }

        [HttpGet("interest/{interest}")]
        public async Task<IActionResult> GetInterestRecommendations([FromRoute] string interest)
        {
            return Ok(await Client.GetInterestRecommendationsAsync(interest));
        }
    }
}