﻿using AdventureWorks.SkiResort.Infrastructure.Model;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;

namespace AdventureWorks.SkiResort.Infrastructure.AzureSearch
{
    public class RestaurantsSearchService
    {
        private readonly string _serviceName = string.Empty;
        private readonly string _apiKey = string.Empty;
        private readonly string _indexer = string.Empty;

        public RestaurantsSearchService(IConfigurationRoot configuration)
        {
            _serviceName = configuration["SearchConfig:ServiceName"];
            _apiKey = configuration["SearchConfig:ApiKey"];
            _indexer = configuration["SearchConfig:Indexer"];
        }

        public async Task<List<Restaurant>> GetNearByAsync(int count, double latitude, double longitude)
        {
            // It uses the distance function to favor items that are within ten kilometers of the current location. 
            string uri = $"https://{_serviceName}.search.windows.net/indexes/{_indexer}/docs?api-version=2016-09-01&$top={count}" +
                $"&scoringProfile=nearrestaurants&scoringParameter=currentLocation:{latitude},{longitude}";

            using (var _httpClient = new HttpClient())
            {
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);

                var response = await _httpClient.GetAsync(uri);
                var jsonString = await response.Content.ReadAsStringAsync();
                var restaurants = JsonConvert.DeserializeObject<RootObject>(jsonString);
                return restaurants.value.ToList();
            }
        }

        public async Task<List<int>> GetRecommendationsAsync(string searchtext, int count)
        {
            int id = int.Parse(searchtext); // roundtrip through int to ensure it's a number
            string uri = $"https://{_serviceName}.search.windows.net/indexes/{_indexer}/docs/{id.ToString("00")}?api-version=2015-02-28";

            using (var _httpClient = new HttpClient())
            {
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);

                var response = await _httpClient.GetAsync(uri);
                var jsonString = await response.Content.ReadAsStringAsync();
                dynamic result = JsonConvert.DeserializeObject(jsonString);
                var obj = result?.RecommendedIds;
                return obj == null ? new List<int>() : ((JArray)obj).Select(t => int.Parse((string)t)).ToList();
            }
        }

    }

    class RootObject
    {
        public string odatacontext { get; set; }
        public Restaurant[] value { get; set; }
    }

    class SuggestionsRootObject
    {
        public string odatacontext { get; set; }
        public Suggestion[] value { get; set; }
    }

    class Suggestion
    {
        public string searchtext { get; set; }
        public int RestaurantId { get; set; }
    }

}
