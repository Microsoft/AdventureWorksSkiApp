﻿using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AdventureWorks.SkiResort.Infrastructure.DocumentDB.Helpers
{
    public abstract class DocumentDBRequest
    {
        protected string _EndpointUrl = string.Empty;
        protected string _Key = string.Empty;
        private readonly string utc_date = string.Empty;

        public DocumentDBRequest(IConfigurationRoot configuration)
        {
            _EndpointUrl = configuration["DocumentDB:EndpointUri"];
            _Key = configuration["DocumentDB:Key"];
            utc_date = DateTime.UtcNow.ToString("r");
        }

        public async Task<string> ExecuteQuery(string databaseId, string collection, string query)
        {
            return await ExecuteQuery(databaseId, collection, query, new List<DBParameter>());
        }

        public async Task<string> ExecuteQuery(string databaseId, string collection, string query, List<DBParameter> parameters)
        {
            using (var _httpClient = new HttpClient())
            {
                string verb = "POST";
                string resourceType = "docs";
                string resourceLink = $"dbs/{databaseId}/colls/{collection}";

                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/query+json"));
                _httpClient.DefaultRequestHeaders.Add("x-ms-date", utc_date);
                _httpClient.DefaultRequestHeaders.Add("x-ms-version", "2015-08-06");
                _httpClient.DefaultRequestHeaders.Add("x-ms-documentdb-isquery", "True");

                string authHeader = DocumentAuthorization.GenerateMasterKeyAuthorizationSignature(verb, resourceLink, resourceType, _Key, "master", "1.0", utc_date);
                _httpClient.DefaultRequestHeaders.Remove("authorization");
                _httpClient.DefaultRequestHeaders.Add("authorization", authHeader);

                var content = JsonConvert.SerializeObject(
                    new
                    {
                        query = query,
                        parameters =  parameters
                    });

                var stringContent = new StringContent(content, Encoding.UTF8, "application/query+json");
                stringContent.Headers.ContentType.CharSet = "";

                string uri = $"{_EndpointUrl}{resourceLink}/docs";
                var response = await _httpClient.PostAsync(uri, stringContent);
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
