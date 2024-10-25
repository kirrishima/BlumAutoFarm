using Blum.Models;
using Blum.Models.Json;
using Blum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Blum.Core
{
    partial class BlumBot
    {
        public async Task<List<TasksJson.TaskModel>> GetTasksAsync()
        {
            try
            {
                var resp = await _session.TryGetAsync(BlumUrls.GET_TASKS);
                if (resp.RestResponse?.IsSuccessStatusCode != true)
                {
                    _logger.Error(($"{_accountName}", ConsoleColor.DarkCyan), ("Failed to fetch tasks", null));
                    return new List<TasksJson.TaskModel>(); // исправление здесь
                }

                var jsonString = resp.ResponseContent;

                var respJson = JsonSerializer.Deserialize<List<TasksJson.TaskResponse>>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var allTasks = await CollectTasks(respJson);
                _logger.Info(($"{_accountName}", ConsoleColor.DarkCyan), ($"Collected {allTasks.Count} tasks", null));
                return allTasks;
            }
            catch (Exception ex)
            {
                _logger.Error(($"{_accountName}", ConsoleColor.DarkCyan), ($"Get tasks error: {ex.Message}", null));
                return new List<TasksJson.TaskModel>(); // исправление здесь
            }
        }

        private async Task<List<TasksJson.TaskModel>> CollectTasks(List<TasksJson.TaskResponse> respJson)
        {
            var collectedTasks = new List<TasksJson.TaskModel>();

            // Получение внешних данных (возможно стоит вынести в отдельный метод и использовать один раз)
            var externalDataUrl = "https://raw.githubusercontent.com/zuydd/database/main/blum.json";
            var externalDataResponse = (await _session.TryGetAsync(externalDataUrl)).ResponseContent ?? "{}";

            var dataJson = JsonSerializer.Deserialize<TasksJson.ExternalTasksData>(externalDataResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Предикат для фильтрации задач
            bool IsTaskValid(TasksJson.TaskModel task) =>
                task.Kind != "QUEST" &&
                task.Status != "FINISHED" &&
                task.ValidationType == "KEYWORD" ? dataJson?.Tasks?.Any(e => e.Id == task.Id && e.Answer != null) == true : true;

            foreach (var section in respJson)
            {
                switch (section.SectionType)
                {
                    case "HIGHLIGHTS":
                        foreach (var task in section.Tasks)
                        {
                            if (task.Status == "FINISHED") continue;

                            if (task.SubTasks != null)
                            {
                                collectedTasks.AddRange(task.SubTasks.Where(IsTaskValid));
                            }

                            if (task.Type != "PARTNER_INTEGRATION" || (task.Type == "PARTNER_INTEGRATION" && task.Reward != null))
                            {
                                if (task.Status != "FINISHED")
                                {
                                    if (task.ValidationType == "KEYWORD")
                                    {
                                        var keyword = dataJson?.Tasks?.Find(e => e.Id == task.Id)?.Answer;
                                        if (keyword != null)
                                        {
                                            collectedTasks.Add(task);
                                        }
                                    }
                                    else
                                    {
                                        collectedTasks.Add(task);
                                    }
                                }
                            }
                        }
                        break;

                    case "WEEKLY_ROUTINE":
                        foreach (var task in section.Tasks)
                        {
                            collectedTasks.AddRange((task.SubTasks ?? new List<TasksJson.TaskModel>()).Where(IsTaskValid));
                        }
                        break;

                    case "DEFAULT":
                        foreach (var subSection in section.SubSections)
                        {
                            collectedTasks.AddRange(subSection.Tasks.Where(IsTaskValid));
                        }
                        break;

                    default:
                        _logger.Warning(($"{_accountName}", ConsoleColor.DarkCyan), ($"Unknown SectionType: {section.SectionType}", null));
                        break;
                }
            }

            return collectedTasks;
        }

        public async Task<bool> ClaimTaskAsync(string taskId)
        {
            try
            {
                var resp = await _session.TryPostAsync(BlumUrls.GetTaskClaimUrl(taskId), null);
                var respJson = JsonSerializer.Deserialize<TasksJson.ResponseStatus>(resp.responseContent ?? "{}", new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return respJson?.Status == "FINISHED";
            }
            catch (Exception ex)
            {
                _logger.Error(($"{_accountName}", ConsoleColor.DarkCyan), ($"Claim section error: {ex.Message}", null));
                return false;
            }
        }

        public async Task StartTaskAsync(string taskId)
        {
            try
            {
                await _session.TryPostAsync(BlumUrls.GetTaskStartUrl(taskId), null);
            }
            catch (Exception ex)
            {
                _logger.Error(($"{_accountName}", ConsoleColor.DarkCyan), ($"Start section error: {ex.Message}", null));
            }
        }

        public async Task<bool> ValidateTaskAsync(string taskId, string title)
        {
            try
            {
                var externalDataUrl = "https://raw.githubusercontent.com/zuydd/database/main/blum.json";
                var externalDataResponse = (await _session.TryGetAsync(externalDataUrl)).ResponseContent ?? "{}";
                var dataJson = JsonSerializer.Deserialize<TasksJson.ExternalTasksData>(externalDataResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var keyword = dataJson?.Tasks?.Find(t => t.Id == taskId)?.Answer;
                if (keyword == null)
                {
                    _logger.Error(($"{_accountName}", ConsoleColor.DarkCyan), ($"Keyword not found for section {taskId}", null));
                    return false;
                }

                var payload = new { keyword };
                var content = JsonSerializer.Serialize(payload);

                var resp = await _session.TryPostAsync(BlumUrls.GetTaskValidateUrl(taskId), content);
                var respJson = JsonSerializer.Deserialize<TasksJson.ResponseStatus>(resp.responseContent ?? "{}", new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (respJson?.Status == "READY_FOR_CLAIM")
                {
                    return await ClaimTaskAsync(taskId);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(($"{_accountName}", ConsoleColor.DarkCyan), ($"Validate section error: {ex.Message}", null));
                return false;
            }
        }

        public async Task Tasks()
        {
            var tasks = await GetTasksAsync();

            foreach (var task in tasks)
            {
                if (task.Status == "NOT_STARTED" && task.Type != "PROGRESS_TARGET" && task.Type != "PROGRESS_TASK")
                {
                    _logger.Info(($"{_accountName}", ConsoleColor.DarkCyan), ($"Started doing task/section - '{task.Title}'", null));
                    await StartTaskAsync(task.Id);
                    await Task.Delay(1000);
                }
            }

            await Task.Delay(5000);

            tasks = await GetTasksAsync();

            foreach (var task in tasks)
            {
                if (!string.IsNullOrEmpty(task.Status))
                {
                    if (task.Status == "READY_FOR_CLAIM" && task.Type != "PROGRESS_TASK")
                    {
                        var status = await ClaimTaskAsync(task.Id);
                        if (status)
                        {
                            _logger.Success(($"{_accountName}", ConsoleColor.DarkCyan), ($"Claimed section - '{task.Title}'", null));
                        }
                        await Task.Delay(500);
                    }
                    else if (task.Status == "READY_FOR_VERIFY" && task.ValidationType == "KEYWORD")
                    {
                        var status = await ValidateTaskAsync(task.Id, task.Title);
                        if (status)
                        {
                            _logger.Success(($"{_accountName}", ConsoleColor.DarkCyan), ($"Validated section - '{task.Title}'", null));
                        }
                    }
                }
            }
        }
    }
}
