namespace Square.TestKit;

public static class TestRunner
{
    public static int Run(params (string Name, Action Test)[] tests)
    {
        int failures = 0;
        foreach ((string name, Action test) in tests)
        {
            try { test(); Console.Out.WriteLine($"PASS {name}"); }
            catch (Exception exception) { failures++; Console.Error.WriteLine($"FAIL {name}: {exception.Message}"); Console.Error.WriteLine(exception); }
        }
        Console.Out.WriteLine($"Executed {tests.Length} test(s); failures: {failures}.");
        return failures == 0 ? 0 : 1;
    }
}
