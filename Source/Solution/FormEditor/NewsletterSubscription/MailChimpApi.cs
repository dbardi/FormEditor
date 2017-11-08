﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FormEditor.NewsletterSubscription
{
	public class MailChimpApi
	{
		public bool Subscribe(string listId, MailAddress email, Dictionary<string, string> mergeFields, string apiKey)
		{
			var apiKeyParts = apiKey.Split('-');
			if(apiKeyParts.Length != 2)
			{
				Log.Warning("The MailChimp API key is invalid; it should contain the MailChimp data center (e.g. 6292043b52144da2a5ccba0ed8dd9016-us15)");
				return false;
			}

			var emailAddress = email.Address;

			var key = apiKeyParts.First();
			var dc = apiKeyParts.Last();
			var hash = GetMd5Hash(emailAddress.ToLowerInvariant());
			var uri = new Uri($"https://{dc}.api.mailchimp.com/3.0/lists/{listId}/members/{hash}");

			var client = new WebClient
			{
				Encoding = Encoding.UTF8
			};

			// set JSON content type
			client.Headers[HttpRequestHeader.ContentType] = "application/json";

			// basic auth, base64 encode of username:password
			var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"-:{key}"));
			client.Headers[HttpRequestHeader.Authorization] = $"Basic {credentials}";

			try
			{
				var data = Serialize(new SubscriptionData
				{
					email_address = emailAddress,
					status = "subscribed",
					merge_fields = mergeFields?.ToDictionary(kvp => kvp.Key.ToUpperInvariant(), kvp => kvp.Value) ?? new Dictionary<string, string>()
				});

				var response = client.UploadString(uri, "PUT", data);
				var result = Deserialize<SubscriptionData>(response);
				return "subscribed".Equals(result.status, StringComparison.OrdinalIgnoreCase) && emailAddress.Equals(result.email_address, StringComparison.OrdinalIgnoreCase);
			}
			catch(WebException wex)
			{
				var responseStream = wex.Response?.GetResponseStream();
				if(responseStream != null)
				{
					using(var reader = new StreamReader(responseStream))
					{
						var response = reader.ReadToEnd();
						Log.Error(wex, $"An error occurred while trying to subscribe the email: {emailAddress}. Error details: {response}", null);
					}
				}
				return false;
			}
			catch(Exception ex)
			{
				Log.Error(ex, $"An error occurred while trying to subscribe the email: {emailAddress}.");
				return false;
			}
		}

		private static string GetMd5Hash(string input)
		{
			using(var md5Hash = MD5.Create())
			{
				var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
				var builder = new StringBuilder();
				foreach(var t in data)
				{
					builder.Append(t.ToString("x2"));
				}
				return builder.ToString();
			}
		}

		private string Serialize<T>(T data)
		{
			return JsonConvert.SerializeObject(data, SerializerSettings());
		}

		private T Deserialize<T>(string data)
		{
			return JsonConvert.DeserializeObject<T>(data, SerializerSettings());
		}

		private static JsonSerializerSettings SerializerSettings()
		{
			// make sure we're serializing with the default contract resolver
			return new JsonSerializerSettings { ContractResolver = new DefaultContractResolver() };
		}

		private class SubscriptionData
		{
			public string email_address { get; set; }

			public string status { get; set; }

			public Dictionary<string, string> merge_fields { get; set; }
		}
	}
}
