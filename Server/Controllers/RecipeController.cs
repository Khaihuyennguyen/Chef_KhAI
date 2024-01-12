using AiChef.Server.Data;
using Chef_KhAI.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Chef_KhAI.Server.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class RecipeController : ControllerBase
	{
		[HttpPost, Route("GetRecipeIdeas")]
		public async Task<ActionResult<List<Idea>>> GetRecipeIdeas()
		{
			return SampleData.RecipeIdeas;
		}
	}
}
