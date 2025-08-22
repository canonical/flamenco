namespace Flamenco.Sandbox.Services;

public class GreetingService
{
    public void Greet(string name = "World")
    {
        Console.WriteLine($"Hello {name}!");
    }
}