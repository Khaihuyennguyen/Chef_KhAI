﻿using System.Text.Json;
using Chef_KhAI.Server.ChatEndPoint;
using Chef_KhAI.Shared;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.IO.Enumeration;

namespace Chef_KhAI.Server.Services
{
	public class OpenAIService : IOpenAIAPI
	{
		private readonly IConfiguration _configuration;
		private static readonly string _baseUrl = "https://api.openai.com/v1/";
		private static readonly HttpClient _httpClient = new();
		private static JsonSerializerOptions _jsonOptions;


		// build the function object so the AI will return JSON formatted object
		private static ChatFunction.Parameter _recipeIdeaParameter = new()
		{
			// describes one Idea
			Type = "object",
			Required = new string[] { "index", "title", "description" },
			Properties = new
			{
				// provide a type and description for each property of the Idea model
				Index = new ChatFunction.Property
				{
					Type = "number",
					Description = "A unique identifier for this object",
				},
				Title = new ChatFunction.Property
				{
					Type = "string",
					Description = "The name for a recipe to create"
				},
				Description = new ChatFunction.Property
				{
					Type = "string",
					Description = "A description of the recipe"
				}
			}
		};

		private static ChatFunction _ideaFunction = new()
		{
			// describe the function we want an argument for from the AI
			Name = "CreateRecipe",
			// this description ensures we get 5 ideas back
			Description = "Generates recipes for each idea in an array of five recipe ideas",
			Parameters = new
			{
				// OpenAI requires that the parameters are an object, so we need to wrap our array in an object
				Type = "object",
				Properties = new
				{
					Data = new // our array will come back in an object in the Data property
					{
						Type = "array",
						// further ensures the AI will create 5 recipe ideas
						Description = "An array of five recipe ideas",
						Items = _recipeIdeaParameter
					}
				}
			}
		};

		public OpenAIService(IConfiguration configuration)
		{

			_configuration = configuration;
			var apiKey = _configuration["OpenAi:OpenAiKey"] ?? Environment.GetEnvironmentVariable("OpenAiKey");

			_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			_httpClient.DefaultRequestHeaders.Authorization = new("Bearer", apiKey);

			_jsonOptions = new()
			{
				PropertyNameCaseInsensitive = true,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
			};
		}

		private static ChatFunction.Parameter _recipeParameter = new()
		{
			Type = "object",
			Description = "The recipe to display",
			Required = new[] { "title", "ingredients", "instructions", "summary" },
			Properties = new
			{
				Title = new
				{
					Type = "string",
					Description = "The title of the recipe to display",
				},
				Ingredients = new
				{
					Type = "array",
					Description = "An array of all the ingredients mentioned in the recipe instructions",
					Items = new { Type = "string" }
				},
				Instructions = new
				{
					Type = "array",
					Description = "An array of each step for cooking this recipe",
					Items = new { Type = "string" }
				},
				Summary = new
				{
					Type = "string",
					Description = "A summary description of what this recipe creates",
				},
			},
		};

		private static ChatFunction _recipeFunction = new()
		{
			Name = "DisplayRecipe",
			Description = "Displays the recipe from the parameter to the user",
			Parameters = new
			{
				Type = "object",
				Properties = new
				{
					Data = _recipeParameter
				},
			}
		};


		public async Task<List<Idea>> CreateRecipeIdeas(string mealtime, List<string> ingrediantList)
		{
			string url = $"{_baseUrl}chat/completions";
			string systemPrompt = "You are a world-renowned chef. I will send you a list of indgredients and a meal time. You will response with 5 ideas for dishes.";
			string userPrompt = "";
			string ingredientPrompt = "";

			string ingredients = string.Join(",", ingrediantList);

			if (string.IsNullOrEmpty(ingredients))
			{
				ingredientPrompt = "Suggest some ingredients for me";

			}
			else
			{
				ingredientPrompt = $"I have {ingredients}";
			}

			userPrompt = $"The meal I want to cook is {mealtime}. {ingredientPrompt}";
			ChatMessage systemMessage = new()
			{
				Role = "system",
				Content = $"{systemPrompt}"

			};
			ChatMessage userMessage = new()
			{
				Role = "user",
				Content = $"{userPrompt}"
			};

			ChatRequest request = new()
			{
				Model = "gpt-3.5-turbo-0613",
				Messages = new[] { systemMessage, userMessage },
				Functions = new[] { _ideaFunction },
				FunctionCall = new { Name = _ideaFunction.Name }
			};

			HttpResponseMessage httpResponse = await _httpClient.PostAsJsonAsync(url, request, _jsonOptions);

			ChatResponse? response = await httpResponse.Content.ReadFromJsonAsync<ChatResponse>();

			//Get the first message in the function c all
			ChatFunctionResponse? functionResponse = response.Choices?
													.FirstOrDefault(m => m.Message?.FunctionCall is not null)?
													.Message?
													.FunctionCall;

			Result<List<Idea>> ideasResult = new();

			if (functionResponse?.Arguments is not null)
				{ 
				try
				{
					ideasResult = JsonSerializer.Deserialize<Result<List<Idea>>>(functionResponse.Arguments, _jsonOptions);
				}

				catch (Exception ex)
				{
					ideasResult = new()
					{
						Exception = ex,
						ErrorMessage = await httpResponse.Content.ReadAsStringAsync()
					};
				}

			}
			
			return  ideasResult?.Data ?? new List<Idea>();
			//return systemPrompt= "You are a world-renowned chefl. I will send you a list of ingredients and a meal time. You will response with 5 ideas for dishes.";
		}
		public async Task<Recipe?> CreateRecipe(string title, List<string> ingrediants)
		{
			string url = $"{ _baseUrl}chat/completions";
			string systemPrompt = "You are a world-renowned chef. Create the recipe with ingredients, instructions and a summary";
			string userPrompt = $"Create a {title} recipe.";

			ChatMessage userMessage = new()
			{
				Role = "user",
				Content = $"{systemPrompt} {userPrompt}"
			};

			ChatRequest request = new()
			{
				Model = "gpt-3.5-turbo-0613",
				Messages = new[] { userMessage },
				Functions = new[] {_recipeFunction},
				FunctionCall = new { Name = _recipeFunction.Name }

			};
			HttpResponseMessage httpResponse = await _httpClient.PostAsJsonAsync(url, request, _jsonOptions);

			ChatResponse? response = await httpResponse.Content.ReadFromJsonAsync<ChatResponse?>();

			ChatFunctionResponse? functionResponse = response.Choices
															 .FirstOrDefault(m => m.Message?.FunctionCall is not null)
															 .Message?
															 .FunctionCall;
			Result<Recipe>? recipe = new();

			if (functionResponse?.Arguments is not null)
			{
				try
				{
					recipe = JsonSerializer.Deserialize<Result<Recipe>>(functionResponse.Arguments, _jsonOptions);

				}
				catch(Exception ex)
				{
					recipe = new()
					{
						Exception = ex,
						ErrorMessage = await httpResponse.Content.ReadAsStringAsync()
					};
				}
			}

			return recipe?.Data;
		}

		public async Task<RecipeImage?> CreateRecipeImage(string recipeTitle)
		{
			string url = $"{_baseUrl}images/generations";
			string userPrompt = "";

			userPrompt = $"Create a restaurant product shot for {recipeTitle}";

			ImageGenerationRequest request = new()
			{
				Prompt = userPrompt
			};

			HttpResponseMessage httpResponse = await _httpClient.PostAsJsonAsync(url, request, _jsonOptions);

			RecipeImage? recipeImage = null;

			try
			{
				recipeImage = await httpResponse.Content.ReadFromJsonAsync<RecipeImage>();

			}
			catch
			{
				Console.WriteLine("Error: Could not retrieve image");
			}
			return recipeImage;
		}
	}
}

