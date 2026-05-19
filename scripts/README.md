# Scripts

The previous single-file database sync script has been replaced by the maintained
console project at `tools/CodeCafe.DbSync`.

Use:

```bash
dotnet run --project tools/CodeCafe.DbSync -- check
dotnet run --project tools/CodeCafe.DbSync -- prod-to-local
dotnet run --project tools/CodeCafe.DbSync -- local-to-test
```
