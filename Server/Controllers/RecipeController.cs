using Chef_KhAI.Server.Data;
using Chef_KhAI.Server.Data;
using Chef_KhAI.Server.Services;
using Chef_KhAI.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Chef_KhAI.Server.Controllers
{
	[Route("[controller]")]
	[ApiController]
	public class RecipeController : ControllerBase
	{
		private readonly IOpenAIAPI _openAIservice;
		public RecipeController (IOpenAIAPI openAIservice)
		{
			_openAIservice = openAIservice;
		}

		
	
		[HttpPost, Route("GetRecipeIdeas")]
		public async Task<ActionResult<List<Idea>>> GetRecipeIdeas(RecipeParms recipeParms)
		{
			string mealtime = recipeParms.MealTime;
			List<string> ingredients = recipeParms.Ingredients
												  .Where(x =>  !string.IsNullOrEmpty(x.Description))
												  .Select(x => x.Description)
												  .ToList();
			if (string.IsNullOrEmpty(mealtime))
			{
				mealtime = "Breakfast";
			}

			var ideas = await _openAIservice.CreateRecipeIdeas(mealtime, ingredients);
			return ideas;
			//return SampleData.RecipeIdeas;
		}

		[HttpPost, Route("GetRecipe")]

		public async Task<ActionResult<Recipe?>> GetRecipe(RecipeParms recipeParms)
		{
			return SampleData.Recipe;
		}
		[HttpGet, Route("GetRecipeImage")]
		public async Task<RecipeImage> GetRecipeImage(string title)
		{
			return SampleData.RecipeImage;
		}
	}
}
