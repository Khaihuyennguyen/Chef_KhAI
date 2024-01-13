using Chef_KhAI.Shared;
using System;
namespace Chef_KhAI.Server.Services
{
	public interface IOpenAIAPI
	{
		Task<List<Idea>> CreateRecipeIdeas(string mealtime, List<string> ingrediants);

	}
}
