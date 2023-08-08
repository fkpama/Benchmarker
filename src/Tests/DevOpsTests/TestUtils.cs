namespace DevOps.Tests
{
    internal static class TestUtils
    {
        internal static string FindToken(string fileName = "pat.txt")
        {
            string fname;
            for (var cur = Environment.CurrentDirectory;
                cur != null;
                cur = Path.GetDirectoryName(cur))
            {
                if (File.Exists(fname = Path.Combine(cur, fileName)))
                {
                    return File.ReadAllText(fname);
                }
            }
            throw new FileNotFoundException("Cannot find PAT token. Check that you have a file name pat.txt with the token above this directory");
        }
    }
}
