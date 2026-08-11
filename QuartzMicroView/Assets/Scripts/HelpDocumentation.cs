using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

public class HelpDocumentation
{
    public string Title { get; set; }
    public string BaseUrl { get; set; }
    public List<Command> Commands { get; set; }
    public string HttpNote { get; set; }
    public string HttpUrl { get; set; }

    public class Command
    {
        public string Name { get; set; }
        public string CommandText { get; set; }
        public string Description { get; set; }

        public override string ToString()
        {
            return $"{Name,-20} {CommandText,-20} {Description}";
        }
    }

    public HelpDocumentation()
    {
        Title = "TCP UDP http control cmdstr Powered by Bility";
        BaseUrl = "http://localhost/control?cmdstr=CommandText";
        HttpNote = "http document generate by apifox:";
        HttpUrl = "https://apifox.com/apidoc/shared-cbffe5ed-2141-4627-bdeb-64706abe6c3c";

        Commands = new List<Command>
        {
            new Command { Name = "PlayVideo", CommandText = "PlayVideo", Description = "" },
            new Command { Name = "PauseVideo", CommandText = "PauseVideo", Description = "" },
            new Command { Name = "StopVideo", CommandText = "StopVideo", Description = "show screensaver when set screensaver" },
            new Command { Name = "PlayNext", CommandText = "PlayNext", Description = "auto skip screensaver" },
            new Command { Name = "PlayPrevious", CommandText = "PlayPrevious", Description = "auto skip screensaver" },
            new Command { Name = "SoundUp", CommandText = "SoundUp", Description = "step 0.1" },
            new Command { Name = "SoundDown", CommandText = "SoundDown", Description = "step 0.1" },
            new Command { Name = "GetVolumn", CommandText = "GetVolumn", Description = "demo: Volumn|0.5 (range-1)" },
            new Command { Name = "SetVolumn by value", CommandText = "SetVolumn|0.5", Description = "" },
            new Command { Name = "PlayVideo by index", CommandText = "PlayVideo|*0", Description = "" },
            new Command { Name = "PlayVideo by filenmae", CommandText = "PlayVideo|abc.mp4", Description = "" },
            new Command { Name = "Loop none", CommandText = "Loop|none", Description = "no loop" },
            new Command { Name = "Loop all", CommandText = "Loop|all", Description = "filelist loop" },
            new Command { Name = "Loop one", CommandText = "Loop|one", Description = "loop one file" },
            new Command { Name = "FileList", CommandText = "FileList", Description = "demo: FileList|0.jpg,1.jpg,2.mp4" },
            new Command { Name = "VideoSeek", CommandText = "VideoSeek|0.5", Description = "range 0-1" },
            new Command { Name = "GetPlayInfo", CommandText = "GetPlayInfo", Description = "demo: PlayInfo|12844.67,42960,1,2024-06-29 13-22-50.mkv\nbackup: PlayInfo|current_ms,all_ms,index,filename" },
            new Command { Name = "SetScreenSaver", CommandText = "SetScreenSaver|abc.jpg", Description = "set screen saver" },
            new Command { Name = "GetScreenSaver", CommandText = "GetScreenSaver", Description = "demo: ScreenSaver|abc.jpg" }
        };
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(Title);
        sb.AppendLine($"Base URL: {BaseUrl}");
        sb.AppendLine();
        sb.AppendLine("Commands:");
        foreach (var command in Commands)
        {
            sb.AppendLine(command.ToString());
        }
        sb.AppendLine();
        sb.AppendLine(HttpNote);
        sb.AppendLine(HttpUrl);
        return sb.ToString();
    }

    public string ToJson()
    {
        return JsonConvert.SerializeObject(this, Formatting.Indented);
    }

    public static HelpDocumentation GetHelpDocument()
    {
        return new HelpDocumentation();
    }
}