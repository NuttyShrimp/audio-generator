using AudioGenerator.Model;

namespace AudioGenerator;

public class Program
{
  public static void Main(string[] args)
  {
    MainAsync().GetAwaiter().GetResult();
  }

  private static async Task MainAsync()
  {
    var generator = new Generator();
    await generator.startFileGeneration();
  }
}