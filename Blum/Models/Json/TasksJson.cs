namespace Blum.Models.Json
{
    public class TasksJson
    {
        public class TaskResponse
        {
            public string SectionType { get; set; }
            public List<TaskModel> Tasks { get; set; }
            public List<SubSection> SubSections { get; set; }
        }

        public class TaskModel
        {
            public string Id { get; set; }
            public string Kind { get; set; }
            public string Type { get; set; }
            public string Status { get; set; }
            public string ValidationType { get; set; }
            // public string IconFileKey { get; set; }
            // public string BannerFileKey { get; set; }
            public string Title { get; set; }
            // public string ProductName { get; set; }
            // public string Description { get; set; }
            public string Reward { get; set; }
            // public SocialSubscription SocialSubscription { get; set; }
            // public bool IsHidden { get; set; }
            // public bool IsDisclaimerRequired { get; set; }
            public List<TaskModel> SubTasks { get; set; }
        }

        public class SubSection
        {
            // public string Title { get; set; }
            public List<TaskModel> Tasks { get; set; }
        }

        /* public class SocialSubscription
         {
             public bool OpenInTelegram { get; set; }
             public string Url { get; set; }
         }*/

        public class ResponseStatus
        {
            public string Status { get; set; }
        }

        public class ExternalTasksData
        {
            public List<ExternalTask> Tasks { get; set; }
        }

        public class ExternalTask
        {
            public string Id { get; set; }
            public string Answer { get; set; }
        }
    }

}
