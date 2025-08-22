using System.Collections.Immutable;
using System.Text;

namespace Flamenco.Console;

public static class Log
{
    private static void Print(string prefix, string message) => System.Console.WriteLine(string.Concat(prefix, message));
    
    private static void Print(string prefix, string message, ConsoleColor textColor)
    {
        System.Console.ForegroundColor = textColor;
        System.Console.WriteLine(string.Concat(prefix, message));
        System.Console.ResetColor();
    }

    public static void Fatal(string message) => Print("[FATAL] ", message, ConsoleColor.DarkRed);
    public static void Error(string message) => Print("[ERROR] ", message, ConsoleColor.Red);
    public static void Warning(string message) => Print("[WARNING] ", message, ConsoleColor.Yellow);
    public static void Info(string message) => Print("[INFO] ", message);
    public static void Debug(string message) => Print("[DEBUG] ", message, ConsoleColor.Gray);

    public static void Annotations(Result result)
    {
        Annotations(result.Annotations, 0);
    }
    
    public static void Annotations(ImmutableList<IAnnotation> annotations, int offset)
    {
        const int tabSize = 5;
        
        foreach (var annotation in annotations)    
        {
            var message = new StringBuilder();
            message.Append(' ', tabSize * offset).Append(annotation.Severity.ToString()).Append(' ').Append(annotation.Identifier).Append(": ").AppendLine(annotation.Title);
            message.Append(' ', 2 + tabSize * offset).AppendLine(annotation.Message);

            foreach (var location in annotation.Locations)
            {
                message.Append(' ', 3 + tabSize * offset).Append("at ").AppendLine(location.ToString());
            }


            if (annotation.Description is not null)
            {
                message.AppendLine();
                message.Append(' ', 2 + tabSize * offset).AppendLine(annotation.Description);
            }

            if (annotation is ExceptionalAnnotation exceptionalAnnotation)
            {
                Exception? exception = exceptionalAnnotation.Exception;

                while (exception is not null)
                {
                    message.AppendLine();
                    message.Append(' ', 2 + tabSize * offset)
                        .Append(exception.GetType().FullName).Append(": ").AppendLine(exception.Message)
                        .AppendLine(exception.StackTrace);

                    exception = exception.InnerException;
                }
            }

            switch (annotation.Severity)
            {
                case AnnotationSeverity.Error:
                    Print(String.Empty, message.ToString(), ConsoleColor.Red);
                    break;
                case AnnotationSeverity.Warning:
                    Print(String.Empty, message.ToString(), ConsoleColor.Yellow);
                    break;
                default:
                    Info(message.ToString());
                    break;
            }

            Annotations(annotation.InnerAnnotations, offset + 1);
        }        
    }
}
