using Chef_KhAI.Shared;

namespace Chef_KhAI.Server.Services
{
	public interface IOpenAIAPI
	{
		Task<List<Idea>> CreateRecipeIdeas(string mealtime, List<string> ingrediants);
		Task<Recipe?> CreateRecipe(string title, List<string> ingrediants);

		Task<RecipeImage?> CreateRecipeImage(string recipeTitle);
	}
}
