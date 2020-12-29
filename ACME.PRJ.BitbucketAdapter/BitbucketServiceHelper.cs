using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace ACME.PRJ.BitbucketAdapter
{
	internal static class BitbucketServiceHelper
	{
        /// <summary>
        /// Генерация строки авторизации в Bitbucket
        /// </summary>
        /// <param name="username">Логин</param>
        /// <param name="password">Пароль</param>
        /// <returns></returns>
		internal static string GetEncodedAuthString(string username, string password)
		{
			var authString = $"{username}:{password}";
			var encodedAuthString = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
			return encodedAuthString;
		}

        /// <summary>
        /// Запрос и получение ответа от Bitbucket'а
        /// </summary>
        /// <param name="encodedAuthString">Строка авторизации</param>
        /// <param name="url">URL запроса</param>
        /// <param name="parameters">Параметры запроса</param>
        /// <returns></returns>
		internal static string GetResponse(string encodedAuthString, string url, string parameters = null)
		{
			if (string.IsNullOrWhiteSpace(url))
				throw new ArgumentNullException(nameof(url));

			string responseJson;
			var normUrl = NormalizeUrl(url);

			if (parameters != null)
				normUrl += $"?{parameters}";

			var webRequest = WebRequest.Create(normUrl);
			webRequest.Method = HttpMethod.Get.Method;
			webRequest.ContentType = "application/json";

			var authHeader = $"Authorization: Basic {encodedAuthString}";
			webRequest.Headers.Add(authHeader);

			using (var resp = webRequest.GetResponse())
			{
				Stream responseStream = resp.GetResponseStream();
				{
					if (responseStream != null)
					{
						using (var reader = new StreamReader(responseStream))
						{
							responseJson = reader.ReadToEnd();
						}
					}
					else
					{
						throw new NullReferenceException(nameof(resp));
					}
				}
			}

			return responseJson;
		}

        /// <summary>
        /// Получение списка значений ответа на запрос
        /// </summary>
        /// <typeparam name="T">Ожидаемый тип значений</typeparam>
        /// <param name="encodedAuthString">Строка авторизации</param>
        /// <param name="url">URL запроса</param>
        /// <param name="parameters">Параметры запроса</param>
		internal static IEnumerable<T> GetValues<T>(string encodedAuthString, string url, string parameters = null)
		{
			IEnumerable<T> result = null;

			if (typeof(T) == typeof(SourceItemDto))
			{
				var responseJson = GetResponse(encodedAuthString, url, $"at={parameters}");
				result = JsonConvert.DeserializeObject<BitbucketBrowseResponse>(responseJson)
					.children.values.Cast<T>();
			}
			else
			{
				var responseJson = GetResponse(encodedAuthString, url, parameters);
				result = JsonConvert.DeserializeObject<BitbucketResponse<T>>(responseJson)
					.values;
			}

			return result;
		}

        /// <summary>
        /// Нормализация строки запроса, чтобы Bitbucket не подавился
        /// </summary>
        /// <param name="url">URL запроса</param>
		private static string NormalizeUrl(string url)
		{
			return url
				.Replace("[", "%5B")
				.Replace("]", "%5D");
		}
	}
}
