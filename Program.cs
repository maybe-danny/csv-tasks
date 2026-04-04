using System.Globalization;

public record struct Task(string Goal, string Date);


public class Runner
{
    public static void Start(TaskManager tm)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine(
                "\n\tl: List of tasks" +
                "\n\tn: New task" +
                "\n\td: Remove task" +
                "\n\tq: Quit"
            );

            Console.Write("\n\tYour choice: ");
            char choice = char.Parse(Console.ReadLine());

            switch (char.ToLower(choice))
            {
                case 'l':
                {
                    Console.Clear();
                    tm.PrintTasks();
                    Console.Write("Press enter to continue ");
                    Console.ReadLine();
                    break;
                }

                case 'n':
                {
                    Console.Clear();
                    Console.WriteLine("Enter your goal");
                    string goal = Console.ReadLine();
                    Console.WriteLine("Enter the end date of the goal in the format dd.mm.yyyy");
                    string date = Console.ReadLine();
                    
                    tm.CreateTask(goal, date);
                    break;
                }

                case 'd':
                {
                    Console.Clear();
                    bool notEmpty = tm.PrintTasks();
                    Console.Write("What task do you want to delete: ");
                    uint index = uint.Parse(Console.ReadLine()) - 1;
                    
                    if (!notEmpty) break;
                    tm.RemoveTask(index);
                    break;
                }

                case 'q':
                {
                    if (tm.Changed())
                    {
                        bool answered = false;
                        while (!answered)
                        {
                            Console.Clear();
                            Console.Write("Save changes? (y/n) ");
                            char key = char.Parse(Console.ReadLine().ToLower());

                            if (key == 'y') 
                            {
                                tm.WriteTasks();
                                answered = true;
                            }
                            else if (key == 'n') answered = true;
                            else continue;
                        }
                    }
                    return;
                }

                default: break;
            }
        }
    }
}

public class TaskManager
{
    private readonly string _path;
    private LinkedList<Task> tasks;
    private bool changed = false;

    public bool Changed() { return changed; }

    public TaskManager(string path)
    {
        this._path = path;
        this.tasks = this.ReadFromCsv();
    }

    private LinkedList<Task> ReadFromCsv()
    {
        LinkedList<Task> result = new LinkedList<Task>();
        bool exists = File.Exists(this._path);

        // Skip reading data if the file did not exist
        if (exists)
        {
            FileStream fileStream = new FileStream(this._path, FileMode.OpenOrCreate, FileAccess.Read);
            StreamReader streamReader = new StreamReader(fileStream);
    
            while (!streamReader.EndOfStream)
            {
                string str = streamReader.ReadLine();
                // Comma index
                int sep = 0;
                for (int i = str.Length - 1; i > 0; i--)
                {
                    if (str[i] == ',')
                    {
                        sep = i;
                        break;
                    }
                }
                // The str.Split method will not work because it can split the string
                // into many parts if there are commas in the Goal field
                // I was lazy and just scan the line from the end until I come across a comma
                string goal = str[0..(sep)].Trim('"');
                string date = str[(sep+1)..(str.Length)];
                result.AddLast(new Task(goal, date));
            }
            streamReader.Close();
            fileStream.Close();
        }
        
        return result;
    }

    private LinkedListNode<Task> GetTaskByIndex(uint index)
    {
        var node = this.tasks.First;
        int i = 0;
        while (node != null && i < index)
        {
            node = node.Next;
            i++;
        }

        return node;
    }

    public bool PrintTasks()
    {
        // Algorithm for calculating the time interval in days
        int DaysBeforeDate(string date)
        {
            DateTime d1 = DateTime.ParseExact(date, "dd.MM.yyyy", CultureInfo.InvariantCulture);
            DateTime d2 = DateTime.Now;

            return Math.Abs((d2 - d1).Days);
        }

        if (tasks.Count == 0)
        {
            Console.WriteLine("No tasks");
            return false;
        }

        int count = 0;
        foreach (Task task in this.tasks)
        {
            count++;
            Console.WriteLine($"| {count}\t| {task.Date}, {DaysBeforeDate(task.Date)} days left \t {task.Goal}");
        }
        return true;
    }

    public void CreateTask(string goal, string date)
    {
        this.changed = true;
        this.tasks.AddLast(new Task(goal, date));
    }

    public void RemoveTask(uint index)
    {
        if (index > this.tasks.Count) return;
        this.tasks.Remove(GetTaskByIndex(index));
        this.changed = true;
    }

    public void WriteTasks()
    {
        FileStream fileStream = new FileStream(this._path, FileMode.Truncate, FileAccess.Write);
        StreamWriter streamWriter = new StreamWriter(fileStream);

        foreach (Task task in this.tasks)
        {
            // "buy beer",01.12.2027
            streamWriter.WriteLine($"\"{task.Goal}\",{task.Date}");
        }

        streamWriter.Close();
        fileStream.Close();
    }
}


internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: The path is not specified.\nWrite the path to .csv document as an argument.");
            return;
        }

        TaskManager tm = new TaskManager(args[0]);
        Runner.Start(tm);
    }
}