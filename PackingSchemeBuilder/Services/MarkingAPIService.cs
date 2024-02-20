using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PackingSchemeBuilder.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PackingSchemeBuilder.Services
{
    public class MarkingAPIService
    {
        private readonly HttpClient _httpClient;
        private const string BaseGetUrl = "http://promark94.marking.by/";

        public MarkingAPIService()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri(BaseGetUrl) };
        }

        public async Task<CurrentTaskInfoModel> GetPackagingMissionInformation()
        {
            try
            {
                string response = await SendGetRequestAsync($"client/api/get/task/");

                CurrentTaskInfoModel currentTaskInfo = JsonConvert.DeserializeObject<CurrentTaskInfoModel>(response);

                return currentTaskInfo;
            }
            catch (HttpRequestException ex)
            {
                HandleRequestException(ex);
            }
            catch (JsonException ex)
            {
                HandleJsonException(ex);
            }
            catch (Exception ex)
            {
                HandleOtherException(ex);
            }

            return null;

           
        }

        private async Task<string> SendGetRequestAsync(string relativeUrl)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(relativeUrl);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                HandleRequestException(ex);
            }
            catch (Exception ex)
            {
                HandleOtherException(ex);
            }

            return null;
        }

        private void HandleRequestException(HttpRequestException ex)
        {
            throw new Exception("Ошибка HTTP-запроса: " + ex.Message, ex);
        }

        private void HandleJsonException(JsonException ex)
        {
            throw new Exception("Ошибка десериализации JSON: " + ex.Message, ex);
        }

        private void HandleOtherException(Exception ex)
        {
            throw new Exception("Произошла ошибка: " + ex.Message, ex);
        }



    }
}
