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

                var allTasks = CollectTasks(respJson);
                _logger.Info(($"{_accountName}", ConsoleColor.DarkCyan), ($"Collected {allTasks.Count} tasks", null));
                return allTasks;
            }
            catch (Exception ex)
            {
                _logger.Error(($"{_accountName}", ConsoleColor.DarkCyan), ($"Get tasks error: {ex.Message}", null));
                return new List<TasksJson.TaskModel>(); // исправление здесь
            }
        }

        private List<TasksJson.TaskModel> CollectTasks(List<TasksJson.TaskResponse> respJson)
        {
            var collectedTasks = new List<TasksJson.TaskModel>();

            foreach (var task in respJson)
            {
                if (task.SectionType == "HIGHLIGHTS")
                {
                    foreach (var t in task.Tasks)
                    {
                        if (t.SubTasks != null)
                        {
                            collectedTasks.AddRange(t.SubTasks);
                        }

                        if (t.Type != "PARTNER_INTEGRATION" || (t.Type == "PARTNER_INTEGRATION" && t.Reward != null))
                        {
                            collectedTasks.Add(t);
                        }
                    }
                }

                if (task.SectionType == "WEEKLY_ROUTINE")
                {
                    foreach (var t in task.Tasks)
                    {
                        collectedTasks.AddRange(t.SubTasks ?? new List<TasksJson.TaskModel>());
                    }
                }

                if (task.SectionType == "DEFAULT")
                {
                    foreach (var subSection in task.SubSections)
                    {
                        collectedTasks.AddRange(subSection.Tasks);
                    }
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
                _logger.Error(($"{_accountName}", ConsoleColor.DarkCyan), ($"Claim task error: {ex.Message}", null));
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
                _logger.Error(($"{_accountName}", ConsoleColor.DarkCyan), ($"Start task error: {ex.Message}", null));
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
                    _logger.Error(($"{_accountName}", ConsoleColor.DarkCyan), ($"Keyword not found for task {taskId}", null));
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
                _logger.Error(($"{_accountName}", ConsoleColor.DarkCyan), ($"Validate task error: {ex.Message}", null));
                return false;
            }
        }

        public async Task Tasks()
        {
            var tasks = await GetTasksAsync();

            foreach (var task in tasks)
            {
                if (task.Status == "NOT_STARTED" && task.Type != "PROGRESS_TARGET")
                {
                    _logger.Info(($"{_accountName}", ConsoleColor.DarkCyan), ($"Started doing task - '{task.Title}'", null));
                    await StartTaskAsync(task.Id);
                    await Task.Delay(500);
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
                            _logger.Success(($"{_accountName}", ConsoleColor.DarkCyan), ($"Claimed task - '{task.Title}'", null));
                        }
                        await Task.Delay(500);
                    }
                    else if (task.Status == "READY_FOR_VERIFY" && task.ValidationType == "KEYWORD")
                    {
                        var status = await ValidateTaskAsync(task.Id, task.Title);
                        if (status)
                        {
                            _logger.Success(($"{_accountName}", ConsoleColor.DarkCyan), ($"Validated task - '{task.Title}'", null));
                        }
                    }
                }
            }
        }
    }
}
