using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WheelOfSteamGames
{
    class TaskManager
    {
        public delegate void OnTaskCompleted(IAsyncResult task);

        private static List<Task> CurrentTasks = new List<Task>();
        public struct Task
        {
            public IAsyncResult Result;
            public string Name;
            public OnTaskCompleted onDoneDel;

            public Task(string name, IAsyncResult result, OnTaskCompleted del)
            {
                Name = name;
                Result = result;
                onDoneDel = del;
            }
        }

        public static void AddTask(IAsyncResult result, OnTaskCompleted del, string name = "none")
        {
            Task task = new Task(name, result, del);
            CurrentTasks.Add(task);
        }

        public static void PollTasks()
        {
            for (int i = 0; i < CurrentTasks.Count; i++)
            {
                Task task = CurrentTasks[i];
                if (!task.Result.IsCompleted) continue;

                task.onDoneDel.Invoke(task.Result);
                if (CurrentTasks.Remove(task)) i--;

            }
        }
    }
}
