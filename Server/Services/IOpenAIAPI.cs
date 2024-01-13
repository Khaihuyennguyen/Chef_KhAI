using Chef_KhAI.Shared;

namespace Chef_KhAI.Server.Services
{
	public class IOpenAIAPI
	{
		Task<List<Idea>> CreateRecipeIdeas(string mealtime, List<string> ingrediants);

	}
}
