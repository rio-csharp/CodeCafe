namespace CodeCafe.DbSync;

internal static class Usage
{
    public static void Print()
    {
        Console.WriteLine("CodeCafe database sync tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project server/tools/CodeCafe.DbSync -- check");
        Console.WriteLine("  dotnet run --project server/tools/CodeCafe.DbSync -- prod-to-local [--yes]");
        Console.WriteLine("  dotnet run --project server/tools/CodeCafe.DbSync -- local-to-test [--yes]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  check          Verify local PostgreSQL client tools and SSH reachability.");
        Console.WriteLine("  prod-to-local  Dump production through an SSH tunnel and restore to local.");
        Console.WriteLine("  local-to-test  Dump local, upload to test, back up test, and restore test.");
        Console.WriteLine();
        Console.WriteLine("Migrations are applied by the deployed api-migrate job");
        Console.WriteLine("  (dotnet CodeCafe.Server.dll migrate); see docs/backend-best-practices.md.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --yes          Skip destructive-operation confirmation prompts.");
        Console.WriteLine();
        Console.WriteLine("Environment variables:");
        Console.WriteLine("  PROD_HOST, TEST_HOST");
        Console.WriteLine("  PROD_SSH_PORT, PROD_SSH_USER, PROD_DB_PORT, PROD_DB_USER, PROD_DB");
        Console.WriteLine("  TEST_SSH_PORT, TEST_SSH_USER, TEST_DB_PORT, TEST_DB_USER, TEST_DB");
        Console.WriteLine("  LOCAL_HOST, LOCAL_DB_PORT, LOCAL_DB_USER, LOCAL_DB");
        Console.WriteLine("  PROD_DB_PASSWORD, LOCAL_DB_PASSWORD");
        Console.WriteLine("  PROD_SSH_PASSWORD, TEST_SSH_PASSWORD");
        Console.WriteLine("  TEST_PGPASSFILE, TEST_BACKUP_DIR, SSH_KEY_PATHS");
    }
}
